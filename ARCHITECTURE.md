# F5 BIG-IP Network Architecture Diagram

## Overview
This document provides a comprehensive network architecture visualization based on the F5 BIG-IP configuration files (TMSH v17.1.3).

## System Information
- **Version**: 17.1.3
- **Deployment**: High Availability (Failover) Pair
- **Primary Device**: SGP-TCX-MGH-IPC-LB01
- **Secondary Device**: SGP-TCX-MGH-IPC-LB02
- **Location**: Singapore
- **Organization**: TATA Communications

---

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         INTERNET / CLIENT REQUESTS                          │
│                                  (0.0.0.0/0)                                │
└──────────────────────────┬────────────────────────┬──────────────────────────┘
                           │                        │
                    HTTP/HTTPS Traffic      HTTPS Traffic
                           │                        │
        ┌──────────────────┴────────────────────────┴──────────────────┐
        │                                                               │
        │         F5 BIG-IP Load Balancer Cluster (HA Pair)           │
        │                                                               │
        │  ┌────────────────────────────────────────────────────────┐ │
        │  │  SGP-TCX-MGH-IPC-LB01 (Primary)                        │ │
        │  │  Mgmt IP: 100.67.241.163/27                           │ │
        │  │  Self IP: 100.67.239.249/24                           │ │
        │  │  Float IP: 100.67.239.250/24                          │ │
        │  │  HA IP: 100.67.242.67/27                              │ │
        │  └────────────────────────────────────────────────────────┘ │
        │                                                               │
        │  ┌────────────────────────────────────────────────────────┐ │
        │  │  SGP-TCX-MGH-IPC-LB02 (Secondary)                      │ │
        │  │  Mgmt IP: 100.67.241.162/27                           │ │
        │  │  Self IP: 100.67.239.248/24                           │ │
        │  │  Mirror IP: 100.67.239.248                            │ │
        │  │  HA IP: 100.67.242.66/27                              │ │
        │  └────────────────────────────────────────────────────────┘ │
        │                                                               │
        │  ┌────────────────────────────────────────────────────────┐ │
        │  │         Network Interfaces & VLAN Configuration        │ │
        │  │  ─────────────────────────────────────────────────────│ │
        │  │  VLAN 816 (Production/Pro_Vlan):                       │ │
        │  │    - Interface: 1.2                                    │ │
        │  │    - VLAN Tag: 816                                     │ │
        │  │    - STP: Enabled (Cost: 200000)                      │ │
        │  │    - Failsafe: Enabled (45s timeout)                  │ │
        │  │                                                        │ │
        │  │  VLAN 766 (HA/Cluster):                                │ │
        │  │    - Interface: 1.3                                    │ │
        │  │    - VLAN Tag: 766                                     │ │
        │  │    - STP: Enabled (Cost: 200000)                      │ │
        │  │                                                        │ │
        │  │  Interface 1.1: 10G (Reserved)                         │ │
        │  └────────────────────────────────────────────────────────┘ │
        │                                                               │
        └───────────────────┬──────────────────────┬────────────────────┘
                           │                      │
                    VLAN 816 Traffic         HA Sync Traffic
                           │                      │
        ┌──────────────────┴──────────────────────┴──────────────────┐
        │                                                             │
        │            Virtual Servers & Load Balancing Layer         │
        │                                                             │
        └─────────────────────────────────────────────────────────────┘
```

---

## Virtual Server Configuration Map

### Production Services

#### 1. **Web & Portal Services**

```
VIP: 100.67.239.181:80/443 → B2B_API_Pool
  ├─ 100.67.239.6:4000 (API Server 1)
  └─ 100.67.239.7:4000 (API Server 2)

VIP: 100.67.239.236:80/443 → B2B_WEB01_Pool
  ├─ 100.67.239.6:4010 (Web Server 1)
  └─ 100.67.239.7:4010 (Web Server 2)

VIP: 100.67.239.235:80/443 → BO_WEB_Pool (Back Office)
  ├─ 100.67.239.13:4001 (BO Web 1)
  └─ 100.67.239.14:4001 (BO Web 2)
  └─ LB Mode: Least Connections

VIP: 100.67.239.184:80/443 → Bo_Web_Pool (Alternate BO)
  ├─ 100.120.4.15:80 (Remote Server 1 - DOWN)
  └─ 100.120.4.5:80 (Remote Server 2 - DOWN)
  └─ Monitor: ICMP Gateway
```

#### 2. **Extranet Services**

```
VIP: 100.67.239.234:80/443 → Extranet_WEB_Pool
  ├─ 100.67.239.13:4005
  └─ 100.67.239.14:4005
  └─ LB Mode: Least Connections

VIP: 100.67.239.244:80 → Extrenet_API_Pool
  ├─ 100.67.239.129:80
  ├─ 100.67.239.130:80
  ├─ 100.67.239.13:80
  ├─ 100.67.239.14:80
  ├─ 100.67.239.16:80
  └─ 100.67.239.24:80

VIP: 100.67.239.185:80/443 → EXT_WEB_Pool (Extranet CM)
  ├─ 100.67.239.17:80
  ├─ 100.67.239.18:80
  └─ 100.67.239.19:80
  └─ Monitor: ICMP Gateway

VIP: 100.67.239.100:80 → Extrenet_API_XmlSell_Pool
  ├─ 100.67.239.104:80
  ├─ 100.67.239.105:80
  └─ 100.67.239.131:80
```

#### 3. **API Gateway & Booking Services**

```
VIP: 100.67.239.231:80/443 → CM_API_9003 (Channel Manager)
  ├─ 100.67.239.101:9003
  ├─ 100.67.239.103:9003
  ├─ 100.67.239.127:9003
  ├─ 100.67.239.25:9003
  ├─ 100.67.239.26:9003
  ├─ 100.67.239.27:9003
  └─ 100.67.239.28:9003
  └─ LB Mode: Least Connections

VIP: 100.67.239.243:80/443 → Gateway_API_Pool
  ├─ 100.67.239.20:5090
  └─ 100.67.239.21:5090

VIP: 100.67.239.247:80 → Booking_API_Pool
  ├─ 100.67.239.132:4004
  ├─ 100.67.239.133:4004
  ├─ 100.67.239.36:4004
  ├─ 100.67.239.6:4004
  └─ 100.67.239.7:4004
  └─ LB Mode: Least Connections

VIP: 100.67.239.182:80/443 → SI_API_Cache_Pool + GATEWAY_Pool
  ├─ Cache Pool:
  │  ├─ 100.67.239.134:80
  │  ├─ 100.67.239.84:80
  │  ├─ 100.67.239.85:80
  │  ├─ 100.67.239.86:80
  │  └─ 100.67.239.87:80
  └─ Gateway Pool:
     ├─ 100.120.4.17:80 (Remote - DOWN)
     └─ 100.120.4.7:80 (Remote - DOWN)
     └─ Monitor: ICMP Gateway
```

#### 4. **Search Integration (SI) Services**

```
VIP: 100.67.239.183:80 → SI_API_Pool (PRIMARY - 26 members)
  ├─ 100.67.239.108:80
  ├─ 100.67.239.109:80
  ├─ 100.67.239.135:80 through 100.67.239.148:80
  └─ LB Mode: Least Connections

VIP: 100.67.239.123:80 → SI_Cache_CB (Cache Builder)
  ├─ 100.67.239.110:80
  ├─ 100.67.239.67:80
  └─ 100.67.239.68:80
  └─ LB Mode: Least Connections

VIP: 100.67.239.230:80/443 → SIUI_WEB
  └─ 100.67.239.6:5000 (Single Member)
  └─ Monitor: TCP + HTTP
```

#### 5. **XML Sell Services (Multi-Version)**

```
VIP: 100.67.239.239:80/443 → XmlSell_APIV2_Pool (Jarvis XML Live)
  ├─ 100.67.239.107:80
  ├─ 100.67.239.142:80
  ├─ 100.67.239.62:80
  ├─ 100.67.239.63:80
  ├─ 100.67.239.64:80
  └─ 100.67.239.65:80

VIP: 100.67.239.186:443 → XmlSell_APIV3_Pool
  └─ 100.67.239.7:6003 (Single Member)
  └─ LB Mode: Least Connections

VIP: 100.67.239.237:80/443 → XmlSell_APIV4_Pool (Search)
  ├─ 100.67.239.106:80
  ├─ 100.67.239.113:80
  ├─ 100.67.239.120:80
  ├─ 100.67.239.143:80 through 100.67.239.148:80
  ├─ 100.67.239.46:80 through 100.67.239.60:80
  └─ LB Mode: Least Connections

VIP: 100.67.239.152:80/443 → XmlSell_HBED_APIV4_Pool (HotelBeds)
  ├─ 100.67.239.114:80
  ├─ 100.67.239.115:80
  ├─ 100.67.239.117:80
  ├─ 100.67.239.118:80
  ├─ 100.67.239.119:80
  ├─ 100.67.239.139:80
  ├─ 100.67.239.141:80
  └─ 100.67.239.47:80
```

#### 6. **CMS & STS Authentication**

```
VIP: 100.67.239.232:80/443 → CMS_API_Pool
  ├─ 100.67.239.8:8888
  └─ 100.67.239.9:8888

VIP: 100.67.239.233:80/443 → STS_WEB_Pool (Auth/STS Web)
  ├─ 100.67.239.8:5000
  └─ 100.67.239.9:5000
  └─ Persistence: Hash-based
```

#### 7. **Notification & PDF Services**

```
VIP: 100.67.239.242:80/443 → Notification_API_Pool
  ├─ 100.67.239.10:5757
  ├─ 100.67.239.111:5757
  └─ 100.67.239.9:5757

VIP: 100.67.239.241:80 → PDF_API_Pool
  ├─ 100.67.239.10:7080
  └─ 100.67.239.11:7080
```

#### 8. **Analytics & Reporting**

```
VIP: 100.67.239.245:80/443 → Report_Pool
  └─ 100.67.240.15:8080

VIP: 100.67.239.246:9200 → int_Ela_search (Elasticsearch)
  ├─ 100.67.240.14:9200
  └─ 100.67.240.15:9200
  └─ Monitor: TCP + HTTP
```

#### 9. **User Management & Other Services**

```
VIP: 100.67.239.240:80 → UM_API_Pool
  ├─ 100.67.239.10:8004
  └─ 100.67.239.11:8004
  └─ Persistence: Hash-based

VIP: 100.67.239.229:80 → new_pool_8080 (XMLSell B2B Web)
  ├─ 100.67.239.140:80
  ├─ 100.67.239.42:80
  ├─ 100.67.239.43:80
  ├─ 100.67.239.44:80
  └─ 100.67.239.45:80
  └─ LB Mode: Least Connections

VIP: 100.67.239.167:80/443 → VIC_Dash_Pool
  └─ 100.67.239.89:80 (Single Member)

VIP: 100.67.239.222:80 → prod-ccapi
  ├─ 100.67.239.11:9004
  └─ 100.67.239.12:9004

VIP: 100.67.239.116:80 → prod-vccapi
  ├─ 100.67.239.11:9003
  └─ 100.67.239.12:9003

VIP: 100.67.239.102:80 → prod-sphinxapi_pool
  ├─ 100.67.239.12:9009
  └─ 100.67.239.13:9009
```

#### 10. **Production External Services**

```
VIP: 100.67.239.150:80/443 → Prod_mgholiday_POOL
  └─ Destination: 100.120.4.37:7108 & 100.120.4.54:7108
  └─ LB Mode: Least Connections
  └─ Purpose: MGHoliday External Service
```

---

### UAT/Stage Environment Virtual Servers

```
VIP: 100.67.239.229 (Multi-port staging gateway)
├─ Port 4001  → stage_backofficeweb_pool_4001
├─ Port 4005  → stage_extranetweb_pool_4005
├─ Port 4010  → stage_B2Bweb_pool_4010
├─ Port 5000  → stage_stsAuth_pool_5000
├─ Port 5090  → stage_gateway_api_5090
├─ Port 5601  → stage_kibana_pool_5601
├─ Port 5757  → stage_notificationapi_5757
├─ Port 6002  → stage_XMLsellV2
├─ Port 6003  → Stage-XmsellV3_pool_6003
├─ Port 6004  → stage_jarvis_pool_6004
├─ Port 7777  → stage_CMS_API_pool_7777
├─ Port 8003  → stage_channel_manager_API_pool_8003
└─ Port 8087  → stage_Demoweb_pool_8087

All Stage pools route to 100.67.239.40 (Staging Server)
Additional: Kibana routes to 100.67.239.41
```

---

## Backend Server Network (100.67.239.0/24 - Production Servers)

### Server Ranges & Functions

| IP Range | Count | Purpose |
|----------|-------|---------|
| .6 - .28 | 23 | Primary Application Servers (APIs, Web) |
| .36 - .89 | 54 | Search Integration (SI) Servers |
| .101 - .148 | 48 | Channel Manager, XML Sell, and Cache Servers |
| .150, .152 | 2 | External Services (mgholiday) |
| .180 - .247 | 68 | Virtual Server Addresses (VIPs) |
| .248 - .249 | 2 | HA/Cluster Addresses |

### Server Groupings by Function

**API Servers** (4000-9009 ports):
- 100.67.239.6-7: B2B API, Booking API, SIUI Web
- 100.67.239.8-9: CMS API, STS Web, Notification API
- 100.67.239.10-11: Notification API, PDF API, UM API
- 100.67.239.12-17: Various Backend APIs
- 100.67.239.20-21: Gateway API
- 100.67.239.101-148: Channel Manager & XML Sell Services

**Search Servers** (SI API - Port 80):
- 100.67.239.37-89: Primary SI API Pool (26 servers)
- 100.67.239.108-110: SI Cache Server Variants
- 100.67.239.134-138: SI API Cache Pool

**External/Remote Servers** (100.120.4.x):
- 100.120.4.5: Back Office Web (Primary - DOWN)
- 100.120.4.7: Gateway (Primary - DOWN)
- 100.120.4.15: Back Office Web (Secondary - DOWN)
- 100.120.4.17: Gateway (Secondary - DOWN)
- 100.120.4.37: mgholiday Service
- 100.120.4.54: mgholiday Service

**Analytics Cluster** (100.67.240.x):
- 100.67.240.14-15: Elasticsearch Cluster (Port 9200)

---

## SSL/TLS Configuration

### Active Certificates
- **MGH_Wildcard_2026**: Current production certificate
- **MGH_COM_2025**: Active alternate certificate
- **MGD_Wildcard_2025**: Intermediate chain
- **MGD_Wildcard_2024**: Legacy certificate

### TLS Profiles
- **MGH_COM_2026**: Modern TLS 1.2/1.3 with cipher group
- **MGH_COM_2025**: TLS 1.2+ (no TLS 1.3)
- **MGH_COM_2025_TLS**: Enhanced TLS configuration
- **X-Forwarded-for**: Custom HTTP profile with XFF header insertion

### Security Cipher Configuration
```
Allowed: TLSv1.2, TLSv1.3
Disabled: TLSv1, TLSv1.1, RSA-based ciphers, DES, 3DES
Cipher Group: TLSv1_3_1_2
```

---

## Authentication & Security

### TACACS+ Configuration
- **Server**: 115.112.173.69
- **Service**: PPP
- **Purpose**: Remote authentication for admin access
- **Encrypted**: Yes (encrypted password)

### SSH Security (SSH-Security-Config)
- **Key Exchange**: ECDH-SHA2-NISTP256
- **Encryption**: AES256-CTR, AES192-CTR, AES128-CTR
- **HMAC**: HMAC-SHA2-512, HMAC-SHA2-256
- **Compression**: None

### Authentication Policy
- **Kerberos Auth Config**: Default authentication flow
  - Kerberos authentication agent
  - Allow/Deny endings
  - Policy-based access control

---

## Network Traffic Flow Diagram

```
                        ┌─────────────────────────┐
                        │    Internet Clients     │
                        │  (Global Access)        │
                        └────────────┬────────────┘
                                     │
                        ┌────────────┴────────────┐
                        │  DNS Resolution & SSL  │
                        │    Termination (F5)    │
                        └────────────┬────────────┘
                                     │
            ┌────────────────────────┼────────────────────────┐
            │                        │                        │
    ┌───────▼─────────┐     ┌────────▼────────┐     ┌────────▼────────┐
    │  HTTP/80 Rules  │     │ HTTPS/443 Rules │     │  Custom Ports   │
    │  Redirect HTTPS │     │  Load Balancing │     │  API Gateway    │
    └───────┬─────────┘     └────────┬────────┘     └────────┬────────┘
            │                        │                        │
            └────────────────────────┼────────────────────────┘
                                     │
                    ┌────────────────┴────────────────┐
                    │     Virtual Servers (VIPs)     │
                    │   100.67.239.180-247 Range      │
                    └────────────────┬────────────────┘
                                     │
         ┌───────────────────────────┼───────────────────────────┐
         │                           │                           │
    ┌────▼─────┐  ┌────────────┐  ┌─▼──────────┐  ┌─────────────▼───┐
    │  HTTP    │  │  iRule    │  │  Pool LB   │  │  Persistence &  │
    │ Profiles │  │ Processing│  │  Algorithm │  │  Session Mgmt   │
    └────┬─────┘  └────────────┘  └─┬──────────┘  └────────────┬────┘
         │                          │                           │
         └──────────────────────────┼───────────────────────────┘
                                    │
                ┌───────────────────┴───────────────────┐
                │     Backend Server Selection         │
                │   (Least Conn, Round Robin, etc)     │
                └───────────────────┬───────────────────┘
                                    │
        ┌───────────────────────────┴───────────────────────────┐
        │                                                         │
    ┌───▼────────────────┐  ┌──────────────────────┐  ┌─────────▼──────┐
    │ Production Servers │  │  Search Servers      │  │ Remote Servers │
    │ 100.67.239.6-28    │  │ 100.67.239.36-148   │  │ 100.120.4.x    │
    │ (APIs, Web)        │  │ (SI, CM, Cache)     │  │ (External)     │
    └────────────────────┘  └──────────────────────┘  └────────────────┘
        │                        │                        │
        └────────────────────────┼────────────────────────┘
                                 │
                    ┌────────────┴────────────┐
                    │                         │
                ┌───▼───────────┐  ┌─────────▼────┐
                │ Analytics     │  │ Response     │
                │ Monitoring    │  │ Processing   │
                │ Logging       │  │ & Return     │
                └───────────────┘  └──────────────┘
```

---

## Management & Monitoring

### Management Network
- **Mgmt Interface**: 100.67.241.163/27 (Primary)
- **Secondary**: 100.67.241.162/27
- **Gateway**: 100.67.241.161
- **Access**: SSH (Port 22), HTTPS (Port 443)

### SNMP Monitoring
- **Community Strings**: Multiple sources configured
  - Public community (default)
  - Private community (1Ln5v@c05M) with restricted OID access
  - Sources: 10.72.212.61, 10.72.212.62, 100.111.2.167, 10.14.0.253

### NTP Synchronization
- **Primary**: 63.243.200.132
- **Secondary**: 115.112.173.67
- **Zone**: Singapore

### Remote Syslog
- **Server**: 10.72.212.45:6431

---

## High Availability Configuration

### Cluster Details
```
Device Group: SYNC (Sync-Failover)
├─ Primary: SGP-TCX-MGH-IPC-LB01
│  └─ Traffic Group 1: Active
│  └─ Local Only: HA Network
│
└─ Secondary: SGP-TCX-MGH-IPC-LB02.tcl.com
   └─ Standby
   └─ State Mirroring: Enabled

Sync Status: Auto-Sync Enabled
Failover Mechanism: Network & State-based
Mirror Address: 100.67.239.249 (Primary)
```

### Connection Mirroring
- **Primary Mirror IP**: 100.67.242.67
- **Secondary Mirror IP**: 100.67.239.249
- **State Synchronization**: Enabled

### Failsafe Configuration
```
VLAN: Pro_Vlan (816)
├─ Failsafe: Enabled
├─ Action: Automatic Failover
├─ Timeout: 45 seconds
└─ Monitor: STP + ICMP
```

---

## iRules & Traffic Processing

### Active iRules
1. **https_redirection_100.67.239.231**: Redirects HTTP to HTTPS for CM API
2. **mgbedbank_redirection**: Redirects mgholiday.com to www.mgbedbank.com
3. **rule_X_Forwarded**: Inserts X-Forwarded-Proto header for HTTPS
4. **rule_X_fwd_for**: Manages X-Forwarded-For header
5. **rules_vicdash_forwarderfor**: Adds client IP to X-Forwarded-For for VIC Dashboard

---

## Service Ports Summary

| Service | Ports | Count | Backends |
|---------|-------|-------|----------|
| Web/Portal | 80, 443 | 10+ VIPs | Multiple pools |
| API Gateway | 5090, 5757, 8004 | 3 | Backend APIs |
| Search (SI) | 80 | 26+ | SI Servers |
| XML Sell | 6002, 6003, 6004, 80, 443 | 4 | Multiple versions |
| CMS | 8888, 7777, 443 | 2 | CMS Servers |
| Auth/STS | 5000, 443 | Multiple | STS Servers |
| PDF/Reports | 7080, 8080, 443 | 2 | PDF/Report Servers |
| Analytics | 9200 | 1 | Elasticsearch |
| Sphinx Search | 9009 | 1 | Sphinx Pool |
| Cache | 80 | Variable | Cache Servers |

---

## Network Segments Summary

```
Management Network:    100.67.241.0/27  (Admin Access)
Production Network:    100.67.239.0/24  (VIPs & Backend)
  ├─ VIPs:            100.67.239.100-247
  ├─ Servers:         100.67.239.6-148
  └─ Cluster:         100.67.239.248-250

HA Cluster Network:    100.67.242.0/27  (State Sync)
Remote Network:        100.120.4.0/22   (External Services)
Analytics Network:     100.67.240.0/24  (Elasticsearch)
Management Routes:     115.112.173.64/27 (NTP/Auth)
                       63.243.200.132/32  (External NTP)
                       100.111.2.167/32   (AIOPS Monitoring)
```

---

## Performance & Optimization

### Compression Profiles
- **HTTP Compression**: GZIP Level 6
- **Buffer**: 4KB
- **Min Size**: 1KB
- **Content Types**: Images, XML, JSON, JavaScript
- **HTTP/2**: Not explicitly configured

### Connection Management
- **Connection Pooling**: OneConnect enabled
- **Chunk Transfer**: Sustain mode
- **Persistence**: Hash-based for stateful services

### Monitoring Health Checks
- **TCP Monitor**: Standard TCP port checks
- **HTTP Monitor**: Application-level HTTP checks
- **ICMP Gateway**: Ping-based monitoring for remote servers

---

## Security Features

### DDoS Protection
- **IPsec IKE Daemon**: Configured
- **UDP/IPv6 Extension Headers**: Blocked
- **Protocol Inspection**: Enabled

### Firewall Rules
- **Self Allow Rules**: TCP/UDP for management ports
- **Default Action**: Accept established connections
- **Protected Ports**: SSH(22), DNS(53), SNMP(161), HTTPS(443)

### SSL Orchestration
- **Certificate Validation**: Enabled
- **OCSP Stapling**: Available
- **Session Tickets**: Disabled (security hardened)

---

## Configuration File Summary

| Component | Quantity | Type |
|-----------|----------|------|
| Virtual Servers | 65+ | Production & Staging |
| Pools | 48+ | Service-specific |
| Nodes | 150+ | Backend Servers |
| iRules | 5 | Traffic Control |
| Profiles | 20+ | HTTP, SSL, TCP |
| Monitors | 5 | Health checks |
| Certificates | 12 | SSL/TLS |
| VLANs | 2 | Prod, HA |
| Device Group | 1 | Failover Pair |

---

## Key Insights

1. **Highly Distributed**: 150+ backend servers across multiple services
2. **Comprehensive Load Balancing**: Multiple LB algorithms (Least Connections, Round Robin)
3. **Robust High Availability**: Active-Standby pair with state mirroring
4. **Modern Security**: TLS 1.2/1.3, certificate management, TACACS+ auth
5. **Multi-Environment**: Production, Staging, and UAT services on same cluster
6. **API-Centric**: Heavy emphasis on API pools (SI, XML Sell, Gateway)
7. **Global Services**: Support for external services and remote data centers
8. **Monitoring-Ready**: SNMP, syslog, and custom health checks

---

*Generated from F5 BIG-IP Configuration Analysis*  
*System: SGP-TCX-MGH-IPC-LB01 v17.1.3*  
*Last Updated: 2024*
