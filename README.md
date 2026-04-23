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
- **Request progress** - is exposed as a percentage via status polling
- **Fault tolerance** - each request is guaranteed to be executed completely unless the timeout exceeded
- **Result caching** — identical requests return cached results without recomputation
- **Configurable alphabet** — uses lowercase Latin letters and digits by default

Limitations
----

* Only MD5 hashing algorithm is supported
* Default configuration searches through strings which consist of lowercase English alphabet and digits (a-z 0-9).

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
    "status": "IN_PROGRESS" | "PENDING",
    "data": null,
    "progress": 0.345123
}
```

When request is completed:
```http
{
    "status": "READY",
    "data": ["result"], // might be empty array if nothing was found
    "progress": 1.0
}
```

When some of the calculations successfully finished and some failed::
```http
{
    "status": "READY_WITH_FAULTS",
    "data": ["result"], // might be empty array
    "progress": 0.123456
}
```

When a critical error has occured:
```http
{
    "status": "ERROR",
    "data": null,
    "progress": 0.123456
}
```


## Internal API

Manager and workers communicate primarily via RabbitMQ message broker. The following describes both the primary transport (RabbitMQ) and a fallback HTTP API for debugging purposes.

---

### Manager to Worker

**Type:** Task assignment

#### Message Format (XML)

The message follows this XSD schema:

<details>
<summary>Show XSD schema</summary>

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
                        <xs:documentation>MD5 hash to crack</xs:documentation>
                    </xs:annotation>
                </xs:element>
                <xs:element name="MaxLength" type="xs:int">
                    <xs:annotation>
                        <xs:documentation>Maximum string length to generate</xs:documentation>
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

**Example:**

<details>
<summary>Show example</summary>

```xml
<CrackHashManagerRequest xmlns="http://ccfit.nsu.ru/schema/crack-hash-request">
    <RequestId>550e8400-e29b-41d4-a716-446655440000</RequestId>
    <PartNumber>1</PartNumber>
    <PartCount>4</PartCount>
    <Hash>5d41402abc4b2a76b9719d911017c592</Hash>
    <MaxLength>4</MaxLength>
    <Alphabet>
        <symbols>a</symbols>
        <symbols>b</symbols>
        <symbols>c</symbols>
    </Alphabet>
</CrackHashManagerRequest>
```

</details>



#### Primary Transport: Message Broker (RabbitMQ)

| Property | Value |
|----------|-------|
| Exchange | `tasks.direct` (type: `direct`, durable) |
| Queue | `worker_tasks` (durable) |
| Routing Key | `task.schedule` |
| Delivery Mode | Persistent |
| Consumer Acknowledgement | Manual (`autoAck: false`) |
| Prefetch Count | 1 (fair dispatch) |

**Behavior:**
- Manager publishes tasks to `tasks.direct` exchange with routing key `task.schedule`
- All workers consume from the same `worker_tasks` queue
- Each task is delivered to exactly one worker (round-robin with prefetch=1)
- Worker must acknowledge the task only after successfully publishing the result to the response queue
- On worker failure, unacknowledged tasks are requeued and redistributed

#### Fallback Transport: HTTP API (Not Recommended)

Use only for debugging.

```http
POST /internal/api/worker/hash/crack/task
Content-Type: application/xml
```

**Note:** HTTP fallback does not provide message persistence, automatic retries, or load balancing. Use only as a temporary fallback.

---

### Worker to Manager

**Type:** Result reporting

#### Message Format (XML)

The message follows this XSD schema:

<details>
<summary>Show XSD schema</summary>

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
                        <xs:documentation>Strings that match the hash</xs:documentation>
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

**Example:**

<details>
<summary>Show example</summary>


```xml
<CrackHashWorkerResponse xmlns="http://ccfit.nsu.ru/schema/crack-hash-response">
    <RequestId>550e8400-e29b-41d4-a716-446655440000</RequestId>
    <PartNumber>1</PartNumber>
    <Answers>
        <words>hello</words>
        <words>world</words>
    </Answers>
</CrackHashWorkerResponse>
```

</details>

#### Primary Transport: Message Broker (RabbitMQ)

| Property | Value |
|----------|-------|
| Exchange | `response.direct` (type: `direct`, durable) |
| Queue | `worker_response` (durable) |
| Routing Key | `task.response` |
| Delivery Mode | Persistent |
| Consumer Acknowledgement | Manual (`autoAck: false`) |

**Behavior:**
- Worker publishes result to `response.direct` exchange with routing key `task.response`
- Manager consumes results from `worker_response` queue
- Manager acknowledges the result after successfully processing it
- If acknowledgement fails, the message remains in queue and will be redelivered

#### Fallback Transport: HTTP API (Not Recommended)

Use only for debugging.

```http
PATCH /internal/api/manager/hash/crack/request
Content-Type: application/xml
```

**Note:** HTTP fallback does not provide guaranteed delivery. In case of manager failure, results may be lost. Always prefer the message broker transport.

---

### Queue Configuration Summary

| Purpose | Exchange | Queue | Routing Key | Consumed By |
|---------|----------|-------|-------------|-------------|
| Task distribution | `tasks.direct` | `worker_tasks` | `task.schedule` | Workers |
| Result collection | `response.direct` | `worker_response` | `task.response` | Manager |

### Delivery Guarantees

| Aspect | Guarantee |
|--------|-----------|
| Message persistence | Yes (durable queues + persistent messages) |
| At-least-once delivery | Yes (manual acknowledgements) |
| Ordering | No (tasks may be processed out of order) |
| Dead-letter handling | Not implemented (messages are retried indefinitely) |

Technology Stack
----

| Component | Technology |
|-----------|------------|
| **Programming Language** | C# |
| **Framework** | .NET 10, ASP.NET Core |
| **Containerization** | Docker, Docker Compose |
| **Message broker** | RabbitMQ |
| **Database** | MongoDB |
| **Logging** | Serilog |
| **Combinatorics** | Kaos.Combinatorics (v5.0.0) |
| **Version Control** | Git |
| **Model Generation** | .NET built-in XSD tool (`xsd.exe` / `dotnet xsd`) |
| **Build & Run Priority** | Docker Compose |

Architecture
----

![Diagramm](CrackHash.drawio.svg)

Optional UI
----

If you prefer a visual interface over calling the API directly, there's a ready-to-use
web dashboard available:

👉 **[crackhash-ui](https://github.com/IliaBoyaCF/crack-hash-ui)** 

> **Note**: The UI repository is a rapid prototype (vibe-coded) focused on demonstrating
> the system's functionality. It's not meant as an example of production frontend architecture,
> but it works and saves you from writing your own client.
