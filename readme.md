# Technical Discussion Points

Consider using an Azure Service Bus over Azure Storage Queues. The key benefits are implementation of an enterprise scale pub/sub model and support for topics. Each subscription gets it's own copy of the message. By decoupling the `Messages` sent to Topics from the Subscribers the system remains flexible to send events to interested parts of the system. By design, if there are no subscriptions to a topic the messages are not persisted on the queue.

Key points and observations
- Messages for the RecordProcessor are queued to a `Records` Topic. 
- A `RecordProcessor` subscription listens to the `Records` topic; multiple listeners on the `RecordProcessor` subscription enable scale out if needed
    - There are flexible options. In the simplest form, the RecordProcessor limits the speed by controlling the *rate* of how quickly it will pull messages freom the queue.
- The RecordProcessor calls the `API` to process a record.
- The API implementation is opaque to the RecordProcessor allowing the API to be redesigned, optimised or evolve without changes to to the bulk upload solution.
    - It's recommend that the API leverages queues/topics to further implement scale-out and rate limiting to ensuring performance and reliability objectives of the solution.
    - API should emit Events pushlished to the Azure Service Bus on a separate Topic, for example an `ApiEvents` Topic. Applications can subscribe to ApiEvents and optionally filter for interested events.

## Example High Level Flow

```mermaid
sequenceDiagram
    participant Caas
    participant Mesh    
    participant FileIngestor
    participant FileJobStorageDb
    participant Queue
    participant RecordProcessor
    
    
    note over RecordProcessor: Responsible for processing records from queue
    note over RecordProcessor, API: Works with API to process records

    note over FileIngestor: Responsible for process CaaS file enqueue records for later processing
            
    FileIngestor-->>Mesh: Listens for new Files    
    Caas-->>Mesh: Drops File
    activate Mesh
    Mesh->>FileIngestor: New File Recieved for Processing
    deactivate Mesh
    par Ingest Files 
        activate FileIngestor

        FileIngestor-->>+FileJobStorageDb: Log Job Id
        FileJobStorageDb-->-FileIngestor: OK
    
        loop While Records in file To Process                            
            FileIngestor->>Queue: Enqueue Record(s)                     
                Queue-->FileIngestor: Message Queue OK            
            Note over FileIngestor: More efficient to read groups of messages from the queue
        end

        FileIngestor->>+FileJobStorageDb: Update Job Id with totalRecords for processing
        FileJobStorageDb-->>-FileIngestor: OK

        deactivate FileIngestor        
    and Process Records
        
            loop While Message In Queue                            
                RecordProcessor->>Queue: Dequeue Records          
                activate RecordProcessor                          
                activate API
                RecordProcessor->>+API: Call API
                alt API Accepts
                    API-->>RecordProcessor: Accept API Call (202 OK Body operation Id)
                    RecordProcessor->>Queue: Acknowledge Record                    
                    alt API Execution
                        API-->>Queue: ⚡ Record Process Event
                        Queue-->>FileJobStorageDb: Update Jobs
                    else
                        API-->>Queue: ⚡ Record Error Event
                        Queue-->>FileJobStorageDb: Update Jobs (Errors)
                    end                    
                else API Unable to Accept
                    API-->>RecordProcessor: Reject API Call (400 Bad Request/405 Method Not Allowed)
                    RecordProcessor->>Queue: Return Record to Queue
                    
                end
                deactivate API            
                Note over RecordProcessor: More efficient to send messages to the queue in a batches of records
                deactivate RecordProcessor
            end       
    end     
```

