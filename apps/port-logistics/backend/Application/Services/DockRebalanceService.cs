namespace Application.Services;

using Application.DTO;
using Domain.IRepository;
using Domain.Model;

/// <summary>
/// US 4.3.3 - Service for rebalancing dock assignments of approved VVNs
/// </summary>
public class DockRebalanceService
{
    private readonly IVesselVisitNotificationRepository _vvnRepo;
    private readonly IDockRepository _dockRepo;
    private readonly IVesselRepository _vesselRepo;
    private readonly IPhysicalResourceRepository _resourceRepo;
    private readonly IStorageAreaRepository _storageRepo;

    public DockRebalanceService(
        IVesselVisitNotificationRepository vvnRepo,
        IDockRepository dockRepo,
        IVesselRepository vesselRepo,
        IPhysicalResourceRepository resourceRepo,
        IStorageAreaRepository storageRepo)
    {
        _vvnRepo = vvnRepo;
        _dockRepo = dockRepo;
        _vesselRepo = vesselRepo;
        _resourceRepo = resourceRepo;
        _storageRepo = storageRepo;
    }

    /// <summary>
    /// Analyzes current dock allocations and proposes optimized reassignments
    /// </summary>
    public async Task<DockRebalanceProposalDTO> GenerateRebalanceProposal(DockRebalanceRequestDTO request)
    {
        var targetDate = DateTime.Parse(request.Date);

        // Get all approved VVNs for the target date with dock assignments
        var approvedVvns = await GetApprovedVvnsForDate(targetDate);

        if (!approvedVvns.Any())
        {
            return new DockRebalanceProposalDTO
            {
                Date = request.Date,
                RecommendationReason = "No approved vessel visit notifications found for this date."
            };
        }

        // Get viable docks for metric calculations
        var allDocks = await _dockRepo.GetAllAsync();
        var allResources = await _resourceRepo.GetAllAsync();
        var allStorage = await _storageRepo.GetAllAsync();
        var viableDocks = GetViableDocks(allDocks.ToList(), allResources.ToList(), allStorage.ToList());

        // Build current allocations
        var currentAllocations = await BuildAllocations(approvedVvns);
        var currentMetrics = CalculateMetrics(currentAllocations, viableDocks);

        // Generate proposed allocations using rebalancing algorithm
        var proposedAllocations = await RebalanceAlgorithm(approvedVvns);
        
        // Check if proposed is actually different from current
        var hasChanges = proposedAllocations.Any(p => 
        {
            var current = currentAllocations.FirstOrDefault(c => c.VvnId == p.VvnId);
            return current == null || current.AssignedDock != p.AssignedDock;
        });

        // Calculate proposed metrics
        var proposedMetrics = CalculateMetrics(proposedAllocations, viableDocks);

        // Determine if rebalancing is beneficial
        var delayImprovement = currentMetrics.TotalExpectedDelay - proposedMetrics.TotalExpectedDelay;
        var improvementPercent = currentMetrics.TotalExpectedDelay > 0 
            ? (delayImprovement / currentMetrics.TotalExpectedDelay) * 100
            : 0;
        
        var balanceImprovement = proposedMetrics.BalanceScore - currentMetrics.BalanceScore;
        
        // Check specific issues for AC compliance
        var currentDockCount = currentAllocations.Select(a => a.AssignedDock).Distinct().Count();
        var poorBalance = currentMetrics.BalanceScore < 50; // Poor balance if score below 50%
        var excessiveCongestion = currentDockCount == 1 && approvedVvns.Count > 1; // All vessels on one dock
        var avoidableDelays = currentMetrics.TotalExpectedDelay > 0.5; // More than 30min delay
        
        // Build list of detected issues
        var issues = new List<string>();
        if (poorBalance) issues.Add("poor balance");
        if (excessiveCongestion) issues.Add("excessive congestion");
        if (avoidableDelays) issues.Add("avoidable delays");

        string recommendation;
        if (!hasChanges)
        {
            recommendation = "No viable alternative dock assignments found. All docks either lack required resources (cranes, storage) or current allocation is optimal.";
        }
        else if (issues.Any())
        {
            var issuesList = string.Join(", ", issues);
            recommendation = $"Rebalancing recommended due to: {issuesList}. " +
                           $"Delay: {currentMetrics.TotalExpectedDelay:F2}h → {proposedMetrics.TotalExpectedDelay:F2}h. " +
                           $"Balance score: {currentMetrics.BalanceScore:F1} → {proposedMetrics.BalanceScore:F1}.";
        }
        else if (balanceImprovement > 10)
        {
            recommendation = $"Rebalancing recommended for improved load distribution: " +
                           $"Balance score: {currentMetrics.BalanceScore:F1} → {proposedMetrics.BalanceScore:F1}. " +
                           $"Delay: {currentMetrics.TotalExpectedDelay:F2}h → {proposedMetrics.TotalExpectedDelay:F2}h.";
        }
        else
        {
            recommendation = "Current dock allocation is already well-balanced. Rebalancing not recommended.";
        }

        return new DockRebalanceProposalDTO
        {
            Date = request.Date,
            CurrentAllocations = currentAllocations,
            ProposedAllocations = proposedAllocations,
            CurrentMetrics = currentMetrics,
            ProposedMetrics = proposedMetrics,
            RecommendationReason = recommendation
        };
    }

    /// <summary>
    /// Applies confirmed dock reassignments
    /// </summary>
    public async Task<DockRebalanceResultDTO> ApplyRebalancing(DockRebalanceConfirmDTO confirmation)
    {
        var result = new DockRebalanceResultDTO
        {
            Timestamp = DateTime.UtcNow,
            OfficerId = confirmation.OfficerId,
            Success = true
        };

        try
        {
            foreach (var reassignment in confirmation.Reassignments)
            {
                var vvn = await _vvnRepo.GetByIdAsync(reassignment.VvnId);
                if (vvn == null)
                {
                    result.Messages.Add($"VVN {reassignment.VvnId} not found");
                    continue;
                }

                // Apply the reassignment
                var errorMessages = new List<string>();
                vvn.UpdateDockAssignment(reassignment.NewDock, confirmation.OfficerId, errorMessages);
                
                if (errorMessages.Any())
                {
                    result.Messages.AddRange(errorMessages);
                    continue;
                }

                var updateErrors = new List<string>();
                await _vvnRepo.Update(vvn, updateErrors);
                if (updateErrors.Any())
                {
                    result.Messages.AddRange(updateErrors);
                    continue;
                }
                result.ReassignmentsApplied++;
                result.Messages.Add($"VVN {reassignment.VvnId}: {reassignment.OriginalDock} → {reassignment.NewDock}");
            }

            // Calculate final metrics
            var targetDate = DateTime.Parse(confirmation.Date);
            var updatedVvns = await GetApprovedVvnsForDate(targetDate);
            var finalAllocations = await BuildAllocations(updatedVvns);
            
            // Get viable docks for final metrics
            var allDocks = await _dockRepo.GetAllAsync();
            var allResources = await _resourceRepo.GetAllAsync();
            var allStorage = await _storageRepo.GetAllAsync();
            var viableDocks = GetViableDocks(allDocks.ToList(), allResources.ToList(), allStorage.ToList());
            
            result.FinalMetrics = CalculateMetrics(finalAllocations, viableDocks);

        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Messages.Add($"Error applying rebalancing: {ex.Message}");
        }

        return result;
    }

    private async Task<List<VesselVisitNotification>> GetApprovedVvnsForDate(DateTime targetDate)
    {
        var approvedVvns = await _vvnRepo.GetByStatusAsync(VvnStatus.Approved);
        
        return approvedVvns.Where(vvn =>
            vvn.AssignedDockCode != null &&
            vvn.EstimatedTimeOfArrival.HasValue &&
            vvn.EstimatedTimeOfArrival.Value.Date == targetDate.Date
        ).ToList();
    }

    private async Task<List<VesselDockAssignmentDTO>> BuildAllocations(List<VesselVisitNotification> vvns)
    {
        var allocations = new List<VesselDockAssignmentDTO>();

        foreach (var vvn in vvns)
        {
            var vessel = await _vesselRepo.GetByImoNumberAsync(vvn.VesselImoNumber);
            
            allocations.Add(new VesselDockAssignmentDTO
            {
                VvnId = vvn.Id,
                VesselImoNumber = vvn.VesselImoNumber,
                VesselName = vessel?.Name ?? "Unknown",
                EstimatedArrival = vvn.EstimatedTimeOfArrival!.Value,
                EstimatedDeparture = vvn.EstimatedTimeOfDeparture ?? vvn.EstimatedTimeOfArrival.Value.AddHours(4),
                AssignedDock = vvn.AssignedDockCode!,
                ContainerCount = vvn.TotalUnloadingContainers + vvn.LoadingContainerCount,
                EstimatedDelay = 0 // Will be calculated via overlap detection
            });
        }

        return allocations;
    }

    private List<Dock> GetViableDocks(List<Dock> allDocks, List<PhysicalResource> allResources, List<StorageArea> allStorage, string? vesselType = null)
    {
        var viableDocks = new List<Dock>();
        
        foreach (var dock in allDocks)
        {
            // Check STS cranes explicitly assigned to this dock
            var cranes = allResources.Where(r => 
                r.Type != null &&
                r.Type == "STS_CRANE" &&
                r.Status == PhysicalResourceStatus.Active &&
                r.AssignedArea == dock.Code).ToList();
            
            // Check storage that explicitly serves this dock
            var storage = allStorage.Where(s => 
                s.ServedDocks != null && 
                s.ServedDocks.Any(sd => sd.DockCode == dock.Code)).ToList();
            
            // AC3: Check vessel type compatibility if specified
            var vesselTypeAllowed = string.IsNullOrEmpty(vesselType) || 
                dock.AllowedVesselTypes.Any(t => t.Equals(vesselType, StringComparison.OrdinalIgnoreCase));
            
            if (cranes.Any() && storage.Any() && vesselTypeAllowed)
            {
                viableDocks.Add(dock);
            }
        }
        
        return viableDocks;
    }
    
    /// <summary>
    /// Get viable docks filtered by specific vessel type (AC3 compliance)
    /// </summary>
    private List<Dock> GetViableDocksForVessel(List<Dock> allDocks, List<PhysicalResource> allResources, List<StorageArea> allStorage, Vessel? vessel)
    {
        var vesselType = vessel?.VesselType;
        return GetViableDocks(allDocks, allResources, allStorage, vesselType);
    }

    private async Task<List<VesselDockAssignmentDTO>> RebalanceAlgorithm(List<VesselVisitNotification> vvns)
    {
        // Rebalancing algorithm: distribute vessels evenly across viable docks
        // US 4.3.3: Must validate dock has required resources (cranes, storage capacity)
        
        var allDocks = await _dockRepo.GetAllAsync();
        var allResources = await _resourceRepo.GetAllAsync();
        var allStorage = await _storageRepo.GetAllAsync();
        
        // Debug: Log resource availability
        Console.WriteLine($"[DockRebalance] ========================================");
        Console.WriteLine($"[DockRebalance] Total docks: {allDocks.Count()}");
        Console.WriteLine($"[DockRebalance] Total resources: {allResources.Count()}");
        Console.WriteLine($"[DockRebalance] Total storage areas: {allStorage.Count()}");
        
        // List all resources for debugging
        foreach (var res in allResources)
        {
            Console.WriteLine($"[DockRebalance] Resource: {res.Code}, Type={res.Type}, Status={res.Status}, AssignedArea={res.AssignedArea ?? "NULL"}");
        }
        
        // List all storage areas for debugging  
        foreach (var stor in allStorage)
        {
            var servedDockCodes = stor.ServedDocks?.Select(sd => sd.DockCode).ToList() ?? new List<string>();
            Console.WriteLine($"[DockRebalance] Storage: {stor.Identifier}, ServedDocks=[{string.Join(", ", servedDockCodes)}]");
        }
        
        // Get viable docks using extracted method
        var viableDocks = GetViableDocks(allDocks.ToList(), allResources.ToList(), allStorage.ToList());
        
        Console.WriteLine($"[DockRebalance] ----------------------------------------");
        foreach (var dock in allDocks)
        {
            var isViable = viableDocks.Any(vd => vd.Code == dock.Code);
            Console.WriteLine($"[DockRebalance] Dock {dock.Code}: {(isViable ? "VIABLE" : "NOT VIABLE")}");
        }
        Console.WriteLine($"[DockRebalance] Viable docks: {viableDocks.Count} ({string.Join(", ", viableDocks.Select(d => d.Code))})");
        Console.WriteLine($"[DockRebalance] ========================================");

        if (viableDocks.Count <= 1)
        {
            // Only one dock available - return current allocations
            Console.WriteLine($"[DockRebalance] Only {viableDocks.Count} dock(s) available, returning current allocations");
            return await BuildAllocations(vvns);
        }

        var rebalanced = new List<VesselDockAssignmentDTO>();
        var dockLoads = viableDocks.ToDictionary(d => d.Code, d => 0); // Track workload per dock

        // Sort VVNs by arrival time for better temporal distribution
        var sortedVvns = vvns.OrderBy(v => v.EstimatedTimeOfArrival).ToList();

        foreach (var vvn in sortedVvns)
        {
            var vessel = await _vesselRepo.GetByImoNumberAsync(vvn.VesselImoNumber);
            var workload = vvn.TotalUnloadingContainers + vvn.LoadingContainerCount;
            workload = workload > 0 ? workload : 50; // Default workload if no containers specified

            // AC3: Get viable docks filtered by this vessel's type
            var vesselViableDocks = GetViableDocksForVessel(allDocks.ToList(), allResources.ToList(), allStorage.ToList(), vessel);
            
            // Find viable dock with lowest current load that supports this vessel type
            var compatibleDockCodes = vesselViableDocks.Select(d => d.Code).ToHashSet();
            var bestDock = dockLoads
                .Where(kv => compatibleDockCodes.Contains(kv.Key))
                .OrderBy(kv => kv.Value)
                .FirstOrDefault();
            
            string assignedDock;
            if (bestDock.Key != null)
            {
                assignedDock = bestDock.Key;
                dockLoads[assignedDock] += workload;
            }
            else
            {
                // No compatible dock found, keep current assignment
                assignedDock = vvn.AssignedDockCode ?? viableDocks.First().Code;
                Console.WriteLine($"[DockRebalance] WARNING: No compatible dock for vessel {vvn.VesselImoNumber} (type: {vessel?.VesselType}), keeping {assignedDock}");
            }
            
            Console.WriteLine($"[DockRebalance] Assigning VVN {vvn.Id} (vessel {vvn.VesselImoNumber}, type: {vessel?.VesselType}) to {assignedDock} (was {vvn.AssignedDockCode})");

            rebalanced.Add(new VesselDockAssignmentDTO
            {
                VvnId = vvn.Id,
                VesselImoNumber = vvn.VesselImoNumber,
                VesselName = vessel?.Name ?? "Unknown",
                EstimatedArrival = vvn.EstimatedTimeOfArrival!.Value,
                EstimatedDeparture = vvn.EstimatedTimeOfDeparture ?? vvn.EstimatedTimeOfArrival.Value.AddHours(4),
                AssignedDock = assignedDock,
                ContainerCount = vvn.TotalUnloadingContainers + vvn.LoadingContainerCount,
                EstimatedDelay = 0 // Will be calculated in CalculateMetrics
            });
        }

        return rebalanced;
    }

    private DockRebalanceMetricsDTO CalculateMetrics(List<VesselDockAssignmentDTO> allocations, List<Dock> viableDocks)
    {
        var metrics = new DockRebalanceMetricsDTO();

        if (!allocations.Any())
            return metrics;

        // Calculate dock utilization
        metrics.DockUtilization = allocations
            .GroupBy(a => a.AssignedDock)
            .ToDictionary(g => g.Key, g => g.Count());

        // Calculate congestion (workload per dock)
        metrics.DockCongestion = allocations
            .GroupBy(a => a.AssignedDock)
            .ToDictionary(g => g.Key, g => (double)g.Sum(v => Math.Max(v.ContainerCount, 50)));

        // Calculate delays based on temporal overlaps
        metrics.TotalExpectedDelay = CalculateOverlapDelays(allocations);
        metrics.VesselsWithDelay = allocations.Count(a => a.EstimatedDelay > 0);
        metrics.AverageDelayPerVessel = allocations.Any() ? metrics.TotalExpectedDelay / allocations.Count : 0;

        // Balance score: 0-100, based on standard deviation of dock loads across ALL viable docks
        if (viableDocks.Any())
        {
            // Get load for each viable dock (0 if no vessels assigned)
            var loads = viableDocks.Select(dock => 
            {
                var dockLoad = metrics.DockCongestion.ContainsKey(dock.Code) 
                    ? metrics.DockCongestion[dock.Code] 
                    : 0.0;
                return dockLoad;
            }).ToList();
            
            var mean = loads.Average();
            var variance = loads.Sum(l => Math.Pow(l - mean, 2)) / loads.Count;
            var stdDev = Math.Sqrt(variance);
            
            // Lower stdDev = better balance. Normalize to 0-100 scale
            // When all vessels on one dock, stdDev will be high, giving low score
            metrics.BalanceScore = mean > 0 ? Math.Max(0, 100 - (stdDev / mean * 100)) : 100;
        }
        else
        {
            metrics.BalanceScore = 100;
        }

        return metrics;
    }

    private double CalculateOverlapDelays(List<VesselDockAssignmentDTO> allocations)
    {
        double totalDelay = 0;

        // Group by dock and calculate overlaps
        foreach (var dockGroup in allocations.GroupBy(a => a.AssignedDock))
        {
            var vesselsList = dockGroup.OrderBy(v => v.EstimatedArrival).ToList();
            
            // Check each pair of consecutive vessels at same dock
            for (int i = 0; i < vesselsList.Count - 1; i++)
            {
                var current = vesselsList[i];
                var next = vesselsList[i + 1];

                // If next vessel arrives before current departs, there's overlap (potential delay)
                if (next.EstimatedArrival < current.EstimatedDeparture)
                {
                    var overlap = (current.EstimatedDeparture - next.EstimatedArrival).TotalHours;
                    
                    // Find the original allocation object and add to its delay
                    var originalNext = allocations.FirstOrDefault(a => a.VvnId == next.VvnId);
                    if (originalNext != null)
                    {
                        originalNext.EstimatedDelay += overlap; // Add overlap delay to existing delay
                        totalDelay += overlap;
                    }
                }
            }
        }

        return totalDelay;
    }
}
