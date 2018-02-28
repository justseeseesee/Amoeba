﻿namespace Amoeba.Rpc
{
    enum AmoebaFunctionType : int
    {
        Exit,
        Cancel,
        GetReport,
        GetNetworkConnectionReports,
        GetCacheContentReports,
        GetDownloadContentReports,
        GetConfig,
        SetConfig,
        SetCloudLocations,
        GetSize,
        Resize,
        CheckBlocks,
        AddContent,
        RemoveContent,
        Diffusion,
        AddDownload,
        RemoveDownload,
        ResetDownload,
        SetProfile,
        SetStore,
        SetMailMessage,
        SetChatMessage,
        GetProfile,
        GetStore,
        GetMailMessages,
        GetChatMessages,
        GetState,
        Start,
        Stop,
        Load,
        Save,
    }
}
