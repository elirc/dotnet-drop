namespace NetDropbox.Domain;

public enum FileNodeType
{
    Folder = 0,
    File = 1
}

public enum FileScanStatus
{
    PendingScan = 0,
    Clean = 1,
    Infected = 2,
    Failed = 3
}

public enum UploadSessionStatus
{
    Open = 0,
    Completed = 1,
    Expired = 2,
    Abandoned = 3
}

public enum SharePermission
{
    View = 0,
    Download = 1,
    EditUpload = 2
}

public enum DirectShareRole
{
    Viewer = 0,
    Editor = 1
}

public enum ProcessingJobKind
{
    VirusScan = 0,
    Thumbnail = 1,
    TextExtraction = 2
}

public enum ProcessingJobStatus
{
    Queued = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    DeadLettered = 4
}

