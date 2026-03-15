# Crack hash

General
----

## Project Description

**CrackHash** is a distributed system for hash cracking using brute-force approach. The system follows a master-worker architecture and consists of two main components:

- **Manager** — accepts client requests, distributes tasks among workers, and aggregates results
- **Worker** — performs the actual brute-force word generation and hash computation

### How It Works
1. Client sends an MD5 hash and maximum word length to the manager
2. Manager generates a unique request ID and splits the search space into equal parts
3. Tasks are distributed among available workers based on part number and total part count
4. Each worker generates word combinations within its assigned range and computes their MD5 hashes
5. Workers return found matches to the manager via HTTP
6. Manager aggregates results and provides them to the client upon request

### Key Features
- **Distributed computing** — scales horizontally by adding more workers
- **Request tracking** — each request gets a unique ID for status polling
- **Result caching** — identical requests return cached results without recomputation
- **Configurable alphabet** — uses lowercase Latin letters and digits by default

Limitations
----

* Only MD5 hashing algorithm is supported
* Default configuration searches through strings which consist of lowercase English alphabet and digits.

Getting Started
----

### Prerequisites

Make sure you have the following installed:

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker](https://docs.docker.com/get-docker/)
- [Docker Compose](https://docs.docker.com/compose/install/)
- [Git](https://git-scm.com/downloads)

### Quick Start (Recommended)

The fastest way to get the system up and running is using Docker Compose:

```bash
# Clone the repository
git clone https://github.com/IliaBoyaCF/crack-hash.git
cd crack-hash

# Build and start all services
docker-compose up --build
```

After startup, the services will be available.

Client API
----

Communication between user and the system happens according to the following protocol:

<details>
<summary>Hash crack request</summary>
Sends request to find strings with length less than or equal to maximum length that will give you the provided MD5 hash.

### Request

```http
POST /api/hash/crack
Content-Type: application/json
```
### Body
```json
{
    "hash": "e2fc714c4727ee9395f324cd2e7f331f", // hash you want to crack
    "maxLength": 4                              // the max length of the string to search
}
```
### Note
The larger the maxLength specified, the longer the computation time will be.

### Response
Will return 200 and GUID of the request to check it's status later on  (see on <code>Hash crack request status check</code>).
```json
{
    "requestId": "730a04e6-4de9-41f9-9d5b-53b88b17afac" // GUID of the request
}
```
</details>
<details>
<summary>Hash crack request status check</summary>
Use the GUID of the request (see <code>Hash crack request</code>) to check its status.

### Request
Provide GUID of the request as a query parameter <code>requestId</code> in your request as follows:

```http
GET /api/hash/status?requestId=730a04e6-4de9-41f9-9d5b-53b88b17afac
```

### Response
If request is queued or in process of computing then the response will be:

```http
{
    "status": "IN_PROGRESS",
    "data": null
}
```

When request is completed:
```http
{
    "status": "READY",
    "data": ["result"] // might be empty array if nothing was found
}
```

When some of the calculations successfully finished and some failed::
```http
{
    "status": "READY_WITH_FAULTS",
    "data": ["result"] // might be empty array
}
```

When a critical error has occured:
```http
{
    "status": "ERROR",
    "data": null
}
```
</details>


Internal API
----
Manager and workers communicate via the following protocol:

<details>
<summary>Manager to worker</summary>
Assign task:

### Request

```http
POST /internal/api/worker/hash/crack/task
Content-Type: application/xml
```

### Body
The body is based on the following xsd-scheme:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
           elementFormDefault="qualified"
           targetNamespace="http://ccfit.nsu.ru/schema/crack-hash-request">
    <xs:element name="CrackHashManagerRequest">
        <xs:annotation>
            <xs:documentation>Request to crack hash in the given string space</xs:documentation>
        </xs:annotation>
        <xs:complexType>
            <xs:sequence>
                <xs:element name="RequestId" type="xs:string">
                    <xs:annotation>
                        <xs:documentation>Request GUID</xs:documentation>
                    </xs:annotation>
                </xs:element>
                <xs:element name="PartNumber" type="xs:int">
                    <xs:annotation>
                        <xs:documentation>Part number of the request</xs:documentation>
                    </xs:annotation>
                </xs:element>
                <xs:element name="PartCount" type="xs:int">
                    <xs:annotation>
                        <xs:documentation>Total number of parts</xs:documentation>
                    </xs:annotation>
                </xs:element>
                <xs:element name="Hash" type="xs:string">
                    <xs:annotation>
                        <xs:documentation>Hash</xs:documentation>
                    </xs:annotation>
                </xs:element>
                <xs:element name="MaxLength" type="xs:int">
                    <xs:annotation>
                        <xs:documentation>Maximum string length</xs:documentation>
                    </xs:annotation>
                </xs:element>
                <xs:element name="Alphabet">
                    <xs:annotation>
                        <xs:documentation>Alphabet for string generation</xs:documentation>
                    </xs:annotation>
                    <xs:complexType>
                        <xs:sequence>
                            <xs:element name="symbols" type="xs:string" minOccurs="0" maxOccurs="unbounded"/>
                        </xs:sequence>
                    </xs:complexType>
                </xs:element>
            </xs:sequence>
        </xs:complexType>
    </xs:element>
</xs:schema>
```

</details>
<details>
<summary>Worker to manager</summary>
Report work results to manager:

### Request

```http
PATCH /internal/api/manager/hash/crack/request
Content-Type: application/xml
```

### Body
The body is based on the following xsd-scheme:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
           targetNamespace="http://ccfit.nsu.ru/schema/crack-hash-response">
    <xs:element name="CrackHashWorkerResponse">
        <xs:annotation>
            <xs:documentation>Response containing strings with matching hash</xs:documentation>
        </xs:annotation>
        <xs:complexType>
            <xs:sequence>
                <xs:element name="RequestId" type="xs:string">
                    <xs:annotation>
                        <xs:documentation>Request GUID</xs:documentation>
                    </xs:annotation>
                </xs:element>
                <xs:element name="PartNumber" type="xs:int">
                    <xs:annotation>
                        <xs:documentation>Request part number</xs:documentation>
                    </xs:annotation>
                </xs:element>
                <xs:element name="Answers">
                    <xs:annotation>
                        <xs:documentation>Strings</xs:documentation>
                    </xs:annotation>
                    <xs:complexType>
                        <xs:sequence>
                            <xs:element name="words" type="xs:string" minOccurs="0" maxOccurs="unbounded"/>
                        </xs:sequence>
                    </xs:complexType>
                </xs:element>
            </xs:sequence>
        </xs:complexType>
    </xs:element>
</xs:schema>
```

</details>

Technology Stack
----

| Component | Technology |
|-----------|------------|
| **Programming Language** | C# |
| **Framework** | .NET 10, ASP.NET Core |
| **Containerization** | Docker, Docker Compose |
| **Logging** | Serilog |
| **Combinatorics** | Kaos.Combinatorics (v5.0.0) |
| **Version Control** | Git |
| **Model Generation** | .NET built-in XSD tool (`xsd.exe` / `dotnet xsd`) |
| **Build & Run Priority** | Docker Compose |

Architecture
----

![Diagramm](CrackHash.drawio.svg)

