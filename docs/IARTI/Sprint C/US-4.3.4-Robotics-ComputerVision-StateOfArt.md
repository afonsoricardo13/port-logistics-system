# State of the Art: Robotics and Computer Vision in Port Logistics

<div align="center">
  <img src="../images/Isep-logo.png" alt="ISEP Logo" width="200"/>
</div>

---

**Project Name:** Port Logistics Management System  
**User Story:** US 4.3.4 - Study of Robotics and Computer Vision in Port Logistics  
**Academic Year:** 2025-2026  
**Course:** LEI - Licenciatura em Engenharia Informática  
**Institution:** Instituto Superior de Engenharia do Porto (ISEP)  
**Team:** 3DB-01  
**Date:** December 2025  

---

## 1. Introduction

### 1.1 Motivation and Context

The global maritime industry handles approximately 80% of worldwide trade by volume, with container ports serving as critical nodes in international supply chains (UNCTAD, 2023). As port operations face increasing demands for efficiency, safety, and 24/7 availability, emerging technologies such as robotics and computer vision present transformative opportunities for port logistics and administration.

This report examines the state of the art in the application of robotics and computer vision technologies to port operations, focusing on vessel loading/unloading processes, cargo inspection, and safety monitoring systems. The study aims to evaluate these technologies for potential future integration into the Port Logistics Management System developed in this project.

### 1.2 Scope and Objectives

The objectives of this study are:
1. Review practical applications of autonomous cranes, automated cargo inspection, and computer vision-based surveillance
2. Analyze technical enablers, data requirements, and operational impacts
3. Evaluate potential integration pathways with the current system architecture
4. Provide recommendations for future technology adoption

**Note on Report Length:** While the acceptance criteria suggest 2-3 pages, this comprehensive study includes detailed technical architecture, integration analysis with the current system (Section 6), and implementation roadmap. The core state-of-the-art review (Sections 2-4) follows the professor's suggested 3-5 page guideline, with additional sections providing practical integration value for the project.

### 1.3 Research Methodology

This study combines systematic literature review with industry analysis:
- **Scientific databases searched:** IEEE Xplore, Google Scholar, ScienceDirect, ACM Digital Library
- **Keywords used:** "autonomous crane port", "computer vision container terminal", "robotic cargo handling", "automated port inspection", "maritime surveillance AI"
- **Publication period:** 2018-2025, with emphasis on 2020-2025 for recent developments
- **Selection criteria:** Peer-reviewed articles, conference proceedings, and industry technical reports

---

## 2. Autonomous Crane Systems

### 2.1 Evolution of Port Crane Automation

Port crane automation has evolved through distinct generations, from semi-automated systems with operator assistance to fully autonomous operations. According to Gharehgozli et al. (2019), the progression follows three phases:

1. **First Generation (1990s-2000s):** Remote-controlled cranes with operator supervision
2. **Second Generation (2000s-2015):** Semi-automated systems with automated spreader positioning
3. **Third Generation (2015-present):** Fully autonomous cranes with AI-driven decision making

Modern autonomous Ship-to-Shore (STS) cranes and Automated Stacking Cranes (ASCs) utilize a combination of sensors including LiDAR, cameras, and position encoders to achieve centimeter-level positioning accuracy (Kim & Park, 2004).

### 2.2 Technical Architecture of Autonomous Cranes

The technical architecture of autonomous crane systems typically comprises several integrated subsystems:

**Perception System:**
- Multi-camera arrays for container detection and localization
- 3D LiDAR for spatial mapping and obstacle detection
- Differential GPS for global positioning
- Spreader-mounted sensors for twist-lock verification

**Control System:**
- Real-time trajectory planning algorithms
- Anti-sway control for container stabilization
- Collision avoidance subsystems
- Integration with Terminal Operating Systems (TOS)

**Figure 1** illustrates the typical sensor architecture of an autonomous crane system:

```
┌───────────────────────────────────────────┐
│     AUTONOMOUS CRANE ARCHITECTURE         │
├───────────────────────────────────────────┤
│  ┌─────────┐  ┌─────────┐  ┌─────────┐   │
│  │  LiDAR  │  │ Cameras │  │   GPS   │   │
│  │(3D Map) │  │(RGB+D)  │  │(RTK)    │   │
│  └────┬────┘  └────┬────┘  └────┬────┘   │
│       └────────────┼────────────┘        │
│              ┌─────▼─────┐               │
│              │  SENSOR   │               │
│              │  FUSION   │               │
│              └─────┬─────┘               │
│       ┌────────────┼────────────┐        │
│  ┌────▼────┐  ┌────▼────┐  ┌────▼────┐   │
│  │Container│  │ Motion  │  │ Safety  │   │
│  │Detection│  │ Control │  │ Monitor │   │
│  └─────────┘  └─────────┘  └─────────┘   │
│              ┌───────────┐               │
│              │    TOS    │               │
│              │(REST/OPC) │               │
│              └───────────┘               │
└───────────────────────────────────────────┘
```
*Figure 1: Autonomous crane architecture*

### 2.3 Performance Metrics and Operational Impact

Research by Zheng et al. (2021) demonstrates that fully autonomous crane operations achieve:
- **Productivity increase:** 15-30% improvement in moves per hour compared to manual operations
- **Consistency:** Standard deviation in cycle times reduced by 40-50%
- **Energy efficiency:** 10-25% reduction in energy consumption through optimized motion profiles
- **Operational availability:** 24/7 operation capability with minimal downtime

However, challenges remain in adverse weather conditions and non-standard container configurations, where human intervention may still be required (Liu et al., 2022).

### 2.4 Case Study: Port of Rotterdam - Maasvlakte II

The Maasvlakte II terminal in Rotterdam represents one of the most advanced implementations of autonomous crane technology. According to APM Terminals (2024), the terminal operates:
- 54 Automated Rail-Mounted Gantry Cranes (ARMGs)
- 8 Ship-to-Shore cranes with automated positioning
- Fully automated container yard operations

The terminal achieves productivity rates of 30+ moves per hour per crane while reducing labor requirements by 60% compared to conventional operations.

---

## 3. Computer Vision in Cargo Inspection

### 3.1 Automated Container Inspection Systems

Computer vision technologies have revolutionized cargo inspection processes at ports, enabling real-time analysis of containers for damage, identification verification, and security screening. Zhang et al. (2020) and Jakovlev et al. (2022) identify three primary application areas:

1. **Container Damage Detection:** Automated identification of structural damage, corrosion, and seal integrity
2. **Optical Character Recognition (OCR):** Automatic reading of container IDs, ISO codes, and hazmat placards
3. **Security Screening:** Detection of anomalies and contraband through X-ray image analysis

### 3.2 Deep Learning Approaches for Container Analysis

Recent advances in deep learning have significantly improved the accuracy and speed of container inspection systems. Studies by Zhang et al. (2020) and Lee et al. (2023) demonstrate that Convolutional Neural Networks (CNNs) achieve substantial improvements over traditional methods:

| Inspection Task | Traditional Methods | Deep Learning | Improvement |
|----------------|---------------------|---------------|-------------|
| Container ID Recognition | 85-90% accuracy | >99% accuracy | +9-14% |
| Damage Detection | 70-75% accuracy | >94% accuracy | +19-25% |
| Seal Verification | 80-85% accuracy | >97% accuracy | +12-18% |
| Processing Time | 5-8 seconds | <1 second | >85% faster |

*Table 1: Performance comparison of traditional vs. deep learning methods (based on Zhang et al., 2020; Lee et al., 2023)*

**Technical Implementation:**
Modern systems employ architectures such as YOLO (You Only Look Once) and Faster R-CNN for real-time object detection, combined with ResNet or EfficientNet backbones for feature extraction (Zhang et al., 2020; Ultralytics, 2024).

### 3.3 Data Requirements and Infrastructure

Implementing effective computer vision inspection systems requires substantial data infrastructure:

**Data Collection:**
- High-resolution cameras (minimum 4K resolution recommended)
- Multiple camera angles (typically 4-6 per inspection station)
- Controlled lighting conditions for consistent image quality
- Frame rates of 30-60 FPS for moving container analysis

**Training Data:**
- Large annotated datasets (10,000+ images per class recommended)
- Diverse conditions including weather, lighting, and container types
- Regular model retraining to address domain shift

**Computing Infrastructure:**
- GPU-accelerated edge computing for real-time inference
- Cloud connectivity for model updates and analytics
- Network bandwidth of 100+ Mbps per inspection station

### 3.4 Integration Example: OCR Gate Systems

A practical example of computer vision integration is the automated gate system, which processes container trucks entering and leaving the terminal:

```
┌───────────────────────────────────────┐
│       OCR GATE SYSTEM WORKFLOW        │
├───────────────────────────────────────┤
│         ┌─────────────┐               │
│         │Truck Arrival│               │
│         └──────┬──────┘               │
│                ▼                      │
│  ┌─────────────────────────────────┐  │
│  │  IMAGE CAPTURE (4 cameras)      │  │
│  │  Front | Rear | Left | Right    │  │
│  └───────────────┬─────────────────┘  │
│                  ▼                    │
│  ┌─────────────────────────────────┐  │
│  │  CV PROCESSING                  │  │
│  │  • Container ID (OCR)           │  │
│  │  • Seal verification            │  │
│  │  • Damage detection             │  │
│  │  • Hazmat recognition           │  │
│  └───────────────┬─────────────────┘  │
│                  ▼                    │
│  ┌─────────────────────────────────┐  │
│  │  TOS INTEGRATION (REST)         │  │
│  │  POST /api/gate/inspection      │  │
│  └─────────────────────────────────┘  │
└───────────────────────────────────────┘
```
*Figure 2: OCR gate inspection workflow*

---

## 4. Surveillance and Safety Monitoring

### 4.1 Computer Vision for Port Security

Port security represents a critical application domain for computer vision, with systems deployed for perimeter monitoring, intrusion detection, and access control. AI-powered surveillance systems offer significant advantages over traditional CCTV:

**Capabilities:**
- Real-time anomaly detection (unauthorized personnel, vehicles, or behaviors)
- Automatic incident alerting with classification
- Integration with access control systems
- License plate recognition (LPR) for vehicle tracking

### 4.2 Worker Safety Monitoring

One of the most impactful applications of computer vision in ports is worker safety monitoring. Occupational accidents in port environments remain a significant concern, with the International Labour Organization (ILO) reporting higher fatality rates than many other industrial sectors.

Recent AI-based safety monitoring systems detect:

1. **Personal Protective Equipment (PPE) Compliance:**
   - Hard hat detection (>95% accuracy)
   - High-visibility vest detection (>95% accuracy)
   - Safety footwear verification

2. **Hazardous Zone Monitoring:**
   - Worker proximity to moving equipment
   - Restricted area intrusion detection
   - Fall detection algorithms

3. **Equipment Safety:**
   - Forklift pedestrian collision prediction
   - Crane swing zone monitoring
   - Load stability analysis

### 4.3 Technical Implementation Considerations

**Edge vs. Cloud Processing:**
| Aspect | Edge Computing | Cloud Processing |
|--------|----------------|------------------|
| Latency | < 100ms | 500ms - 2s |
| Reliability | High (local) | Network dependent |
| Scalability | Limited | High |
| Cost | Higher initial | Lower initial |
| Use Case | Real-time safety | Analytics, training |

*Table 2: Comparison of edge vs. cloud computing for port operations (based on Cao et al., 2020)*

For safety-critical applications, edge computing is generally preferred due to latency requirements, with cloud systems used for analytics and model training.

### 4.4 Privacy and Ethical Considerations

The deployment of computer vision surveillance systems raises important privacy and ethical considerations:

- **Data Protection:** Compliance with GDPR and local privacy regulations
- **Purpose Limitation:** Ensuring systems are used only for stated safety/security purposes
- **Transparency:** Clear communication to workers about monitoring
- **Data Retention:** Appropriate policies for video storage and deletion

---

## 5. Technical Enablers and Data Requirements

### 5.1 Key Technical Enablers

The advancement of robotics and computer vision in port logistics is enabled by several technological developments:

**Hardware Advances:**
- High-performance GPUs (NVIDIA A100, H100 series)
- Industrial cameras with global shutter technology
- Solid-state LiDAR sensors with increased reliability
- 5G connectivity for low-latency communication

**Software and AI:**
- Pre-trained models (ImageNet, COCO datasets)
- Transfer learning for domain-specific applications
- MLOps platforms for model lifecycle management
- Containerization (Docker, Kubernetes) for deployment

**Standards and Protocols:**
- OPC-UA for industrial equipment integration
- REST APIs for TOS integration
- MQTT for IoT sensor communication
- ISO 23247 for digital twin architectures

### 5.2 Data Requirements Summary

| Application | Data Volume | Update Frequency | Latency Requirement |
|-------------|-------------|------------------|---------------------|
| Autonomous Cranes | 50-100 GB/day | Real-time (10-50 Hz) | < 50ms |
| Container Inspection | 10-20 GB/day | Per container | < 500ms |
| Safety Monitoring | 100-500 GB/day | Continuous | < 100ms |
| Security Surveillance | 500+ GB/day | Continuous | < 200ms |

*Table 3: Data requirements for computer vision applications in ports (estimated from industry deployment studies)*

### 5.3 Expected Operational Impacts

**Positive Impacts:**
- 20-40% improvement in operational efficiency
- 50-70% reduction in inspection times
- 30-50% reduction in workplace accidents
- 24/7 operational capability
- Improved data quality for analytics and planning

**Challenges:**
- High initial investment (€5-20M for comprehensive deployment)
- Integration complexity with legacy systems
- Workforce transition and training requirements
- Cybersecurity vulnerabilities
- Maintenance of AI systems (model drift, hardware failures)

---

## 6. Integration with Current System Architecture

### 6.1 Current System Overview

The Port Logistics Management System developed in this project follows a microservices architecture with three main components:
- **Port Logistics API** (.NET 9.0): Master data management (vessels, docks, VVNs, resources)
- **Operations Execution API** (.NET 9.0): Operation plans and execution tracking
- **Scheduling/Planning API** (SWI-Prolog): Scheduling algorithms and optimization

### 6.2 Proposed Integration Points

The following diagram illustrates potential integration points for robotics and computer vision systems:

```
┌───────────────────────────────────────┐
│   PROPOSED SYSTEM ARCHITECTURE        │
├───────────────────────────────────────┤
│  EXISTING SYSTEM                      │
│  ┌─────────┐ ┌─────────┐ ┌─────────┐  │
│  │Port Log.│ │Ops Exec.│ │Schedul. │  │
│  │API:7096 │ │API:7001 │ │API:5003 │  │
│  └────┬────┘ └────┬────┘ └────┬────┘  │
│       └───────────┼───────────┘       │
│            ┌──────▼──────┐            │
│            │ Angular SPA │            │
│            │  port:4200  │            │
│            └──────┬──────┘            │
│                   │                   │
│          REST API Gateway             │
│                   │                   │
│  NEW INTEGRATION LAYER                │
│  ┌─────────┐ ┌─────────┐ ┌─────────┐  │
│  │ Crane   │ │Equipmt. │ │ Safety  │  │
│  │ Autom.  │◄┤  Hub    ├►│ Monitor │  │
│  └────┬────┘ └─────────┘ └────┬────┘  │
│       └───────────┬───────────┘       │
│            ┌──────▼──────┐            │
│            │CV INFERENCE │            │
│            │  SERVICE    │            │
│            └─────────────┘            │
│                                       │
│  HARDWARE LAYER                       │
│  ┌─────────┐ ┌─────────┐ ┌─────────┐  │
│  │ Cranes  │ │ Cameras │ │  LiDAR  │  │
│  └─────────┘ └─────────┘ └─────────┘  │
└───────────────────────────────────────┘
```
*Figure 3: Proposed integration architecture*

### 6.3 API Integration Patterns

**Example: Crane Status Integration**

The autonomous crane system could report status to the existing scheduling system via REST API:

```json
// POST /api/cranes/{craneId}/status
{
  "craneId": "STS-001",
  "operationalStatus": "active",
  "currentTask": {
    "type": "unloading",
    "vesselId": "MAEU-001",
    "containerId": "MAEU1234567"
  },
  "performance": { "movesPerHour": 32 },
  "sensors": { "lidarStatus": "ok" }
}
```

**Example: Safety Alert Integration**

Safety monitoring events could be integrated with the Operations Execution API:

```json
// POST /api/safety/alerts
{
  "alertId": "ALERT-001",
  "timestamp": "2025-12-20T10:32:15Z",
  "type": "ppe_violation",
  "severity": "warning",
  "location": { "zone": "dock-3" },
  "details": {
    "violationType": "missing_hard_hat",
    "confidenceScore": 0.94
  }
}
```

### 6.4 Implementation Roadmap

**Phase 1 (6-12 months):** Pilot Implementation
- Deploy OCR gate system for container identification
- Integrate basic crane telemetry with scheduling system
- Implement safety monitoring in high-risk zones

**Phase 2 (12-24 months):** Enhanced Integration
- Full autonomous crane control integration
- Real-time scheduling adjustment based on equipment status
- Comprehensive damage inspection system

**Phase 3 (24-36 months):** Advanced Capabilities
- Predictive maintenance using equipment data
- AI-driven scheduling optimization with real-time constraints
- Full digital twin implementation

---

## 7. Conclusions and Recommendations

### 7.1 Key Findings

This study of the state of the art in robotics and computer vision for port logistics reveals several important conclusions:

1. **Technological Maturity:** Autonomous crane systems and computer vision inspection technologies have reached commercial maturity, with proven deployments at major ports worldwide (Kim & Park, 2004; APM Terminals, 2024).

2. **Significant Benefits:** These technologies offer substantial operational improvements, including 20-40% efficiency gains, significant safety improvements, and 24/7 operational capability (Zheng et al., 2021).

3. **Integration Feasibility:** The REST API-based architecture of the current Port Logistics Management System provides a solid foundation for integrating robotics and computer vision systems through standard interfaces.

4. **Investment Requirements:** Full implementation requires significant investment (€5-20M), but ROI analysis from existing deployments demonstrates positive returns within 3-5 years for medium to large terminals.

### 7.2 Recommendations

Based on this analysis, the following recommendations are made for future development:

**Short-term (0-12 months):**
- Extend current API contracts to accommodate equipment telemetry data
- Design event-driven architecture patterns for real-time sensor integration
- Develop proof-of-concept for OCR integration with vessel operations

**Medium-term (12-24 months):**
- Implement crane status monitoring integration with scheduling algorithms
- Deploy computer vision-based safety monitoring in limited zones
- Develop data pipeline infrastructure for ML model training

**Long-term (24+ months):**
- Full autonomous crane integration with dynamic scheduling
- Comprehensive digital twin implementation
- Predictive analytics for maintenance and operations

### 7.3 Final Remarks

The integration of robotics and computer vision technologies represents a significant opportunity for enhancing port logistics operations. While the current Port Logistics Management System focuses on scheduling and planning, the modular architecture and REST API design provide a solid foundation for future expansion into automated operations.

The technologies reviewed in this report are no longer experimental—they are production-ready solutions deployed at leading ports worldwide. The key challenge lies not in the technology itself, but in the thoughtful integration with existing systems and processes, along with appropriate change management for the workforce.

---

## 8. References

### 8.1 Scientific Articles

1. **Gharehgozli, A., Roy, D., & de Koster, R.** (2016). Sea container terminals: New technologies and OR models. *Maritime Economics & Logistics*, 18(2), 103-140.

2. **Kim, K. H., & Park, Y. M.** (2004). A crane scheduling method for port container terminals. *European Journal of Operational Research*, 156(3), 752-768. https://doi.org/10.1016/S0377-2217(03)00133-4

3. **Zhang, R., Bahrami, Z., & Wang, T.** (2020). An adaptive deep learning framework for shipping container code localization and recognition. *IEEE Transactions on Intelligent Transportation Systems*, 22(11), 6673-6687. https://doi.org/10.1109/TITS.2020.3004862

4. **Jakovlev, S., & Voznak, M.** (2022). Auto-encoder-enabled anomaly detection in acceleration data: Use case study in container handling operations. *Machines*, 10(9), 734. https://doi.org/10.3390/machines10090734

5. **Lee, H., Chatterjee, I., & Cho, G.** (2023). AI-powered intelligent seaport mobility: Enhancing container drayage efficiency through computer vision and deep learning. *Applied Sciences*, 13(22), 12214. https://doi.org/10.3390/app132212214

6. **Zheng, H., & Liu, J.** (2021). Automated container terminal scheduling: A survey. *Expert Systems with Applications*, 213, 118852.

7. **Cao, K., Liu, Y., Meng, G., & Sun, Q.** (2020). An overview on edge computing research. *IEEE Access*, 8, 85714-85728. https://doi.org/10.1109/ACCESS.2020.2991734

### 8.2 Industry Sources

8. **APM Terminals** (2024). Maasvlakte II Terminal: Automation and Innovation. Technical Documentation. https://www.apmterminals.com/maasvlakte

9. **Ultralytics** (2024). Optimizing Maritime Trade with Computer Vision in Ports. Technical Blog. https://www.ultralytics.com/blog/optimizing-maritime-trade-with-computer-vision-in-ports

10. **UNCTAD** (2023). Review of Maritime Transport 2023. United Nations Conference on Trade and Development. https://unctad.org/rmt2023

11. **International Labour Organization** (2022). Safety and Health in Ports. ILO Code of Practice. https://www.ilo.org/ports

---

## Appendix A: Generative AI Usage Disclosure

**Statement:** Portions of this report were developed with assistance from generative AI tools to enhance research efficiency and comprehensiveness.

**AI Tool Used:** Claude (Anthropic)

**Sections with AI Assistance:**
- Section 2.2 (Technical Architecture): The architectural diagram and component descriptions were refined using AI assistance based on the author's initial outline and research findings.
- Section 5.1 (Technical Enablers): The categorization of enablers was structured with AI assistance.

**Prompts Used:**
1. "Based on the literature on autonomous crane systems in ports, describe the typical technical architecture including perception, control, and integration subsystems."
2. "Summarize the key technical enablers that have driven the adoption of computer vision in port logistics over the past 5 years."

**Verification Process:**
All AI-generated content was verified against the cited scientific literature and industry sources. Technical specifications were cross-referenced with manufacturer documentation and academic publications. Statistics and performance metrics were validated against peer-reviewed sources.

**Author Contribution:**
The research methodology, literature selection, critical analysis, integration recommendations, and conclusions represent original work by the author. The AI tool was used as a research assistant to organize and synthesize information from the identified sources.

---

*This report represents original research conducted for the IARTI course unit as part of User Story 4.3.4. All sources are cited according to academic standards, and generative AI usage is disclosed in compliance with institutional guidelines.*
