# Performance Thinking

Measure first. Then reason about data size, allocation, query shape, and concurrency.

| Hotspot | Evidence | Why it matters | Suggested probe |
| --- | --- | --- | --- |
| Browser whole-file hashing | [sha256](../../../web/src/api.ts#L160-L164) | Allocates full file in memory. | Browser performance memory timeline. |
| Search contains/lower | [FileService.SearchAsync](../../../src/NetDropbox.Application/Files/FileService.cs#L125-L134) | Can full-scan rows. | EF SQL + `EXPLAIN`. |
| Version includes | [FileService.OpenDownloadAsync](../../../src/NetDropbox.Application/Files/FileService.cs#L114-L122) | Large version history loads into memory. | Query count and row count. |
| Worker text sample | [ReadTextSampleAsync](../../../src/NetDropbox.Infrastructure/Processing/ProcessingWorker.cs#L78-L91) | Reads 1MB into memory per job. | Worker memory under concurrent jobs. |
| Parallel upload | [uploadOne workers](../../../web/src/App.tsx#L285-L288) | Client concurrency becomes API load. | Server request rate during multi-file upload. |

Senior habit: make the performance budget explicit before optimizing.

