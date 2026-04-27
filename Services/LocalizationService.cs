using System;
using System.Collections.Generic;
using System.Globalization;
using PureSFTP.Models;

namespace PureSFTP.Services;

public sealed class LocalizationService : ILocalizationService
{
    private static readonly IReadOnlyDictionary<AppLanguage, IReadOnlyDictionary<string, string>> Resources =
        new Dictionary<AppLanguage, IReadOnlyDictionary<string, string>>
        {
            [AppLanguage.English] = new Dictionary<string, string>
            {
                ["WorkspaceHeroTitle"] = "Simple, light and cross-platform SFTP workspace",
                ["SettingsHeroTitle"] = "Configure your workspace and choose the language you prefer",
                ["StatusLabel"] = "STATUS",
                ["WorkspaceNavButton"] = "Workspace",
                ["SettingsNavButton"] = "Settings",
                ["ConnectionMenu"] = "Connection",
                ["FileMenu"] = "File",
                ["BrowserMenu"] = "Browser",
                ["ViewMenu"] = "View",
                ["CancelButton"] = "Cancel",
                ["ConnectionsTitle"] = "Connections",
                ["NewConnectionButton"] = "New",
                ["NewConnectionTitle"] = "New Connection",
                ["NewConnectionDescription"] = "Save a server profile and test it before connecting.",
                ["ConnectionNameLabel"] = "Name",
                ["TestConnectionButton"] = "Test",
                ["SaveConnectionButton"] = "Save",
                ["NoConnectionsHint"] = "No saved connections yet. Create a new connection to start browsing.",
                ["TaskCenterTitle"] = "Task Center",
                ["TaskCenterEmpty"] = "Tasks",
                ["TaskCenterCount"] = "Tasks ({0})",
                ["ConnectionTitle"] = "Connection",
                ["HostLabel"] = "Host",
                ["PortLabel"] = "Port",
                ["UsernameLabel"] = "Username",
                ["PasswordLabel"] = "Password",
                ["StartupPathLabel"] = "Startup path",
                ["HostPlaceholder"] = "sftp.example.com",
                ["UsernamePlaceholder"] = "username",
                ["PasswordPlaceholder"] = "password",
                ["StartupPathPlaceholder"] = "/ or relative path",
                ["ConnectButton"] = "Connect",
                ["DisconnectButton"] = "Disconnect",
                ["ActionsTitle"] = "Actions",
                ["UploadButton"] = "Upload local file",
                ["UploadFolderButton"] = "Upload local folder",
                ["DownloadButton"] = "Download selected item",
                ["DeleteButton"] = "Delete selected item",
                ["CreateFolderHint"] = "Create folder in current path",
                ["CreateFolderPlaceholder"] = "New folder name",
                ["CreateFolderButton"] = "Create folder",
                ["ExplorerTitle"] = "Remote Explorer",
                ["UpButton"] = "Up",
                ["OpenButton"] = "Open",
                ["RefreshButton"] = "Refresh",
                ["NameHeader"] = "Name",
                ["TypeHeader"] = "Type",
                ["SizeHeader"] = "Size",
                ["ModifiedHeader"] = "Modified",
                ["ActivityTitle"] = "Activity",
                ["SettingsTitle"] = "Settings",
                ["SettingsDescription"] = "Switch the interface language here. The preference is stored locally and restored on the next launch.",
                ["LanguageTitle"] = "Language",
                ["LanguageDescription"] = "Choose the display language for the application.",
                ["LanguageLabel"] = "Display language",
                ["LanguagePreviewLabel"] = "Current language",
                ["AppInfoTitle"] = "About PureSftp",
                ["AppInfoDescription"] = "PureSftp focuses on lightweight remote file operations with a clean Avalonia desktop experience across platforms.",
                ["StateWorking"] = "Working",
                ["StateConnected"] = "Connected",
                ["StateOffline"] = "Offline",
                ["ConnectionSummaryOffline"] = "A lightweight, cross-platform SFTP workspace for quick file operations.",
                ["ConnectionSummaryOnline"] = "{0}:{1} as {2}",
                ["SelectionSummaryNone"] = "No item selected.",
                ["SelectionSummaryItem"] = "{0} · {1}",
                ["SelectionSummaryParent"] = "Go to parent directory.",
                ["LogSummaryNone"] = "No activity yet.",
                ["LogSummaryCount"] = "{0} recent events",
                ["DirectoryType"] = "Directory",
                ["FileType"] = "File",
                ["StatusDefault"] = "Fill in your server details and connect when you're ready.",
                ["StatusConnecting"] = "Connecting to the server...",
                ["StatusConnected"] = "Connected to {0}.",
                ["StatusTestingConnection"] = "Testing...",
                ["StatusTestSucceeded"] = "Test succeeded.",
                ["StatusDisconnected"] = "Disconnected.",
                ["StatusDisconnecting"] = "Disconnecting...",
                ["StatusRefreshing"] = "Refreshing remote directory...",
                ["StatusLoaded"] = "Loaded {0}.",
                ["StatusFileReadyToDownload"] = "'{0}' is ready to download.",
                ["StatusOpening"] = "Opening {0}...",
                ["StatusOpened"] = "Opened {0}.",
                ["StatusLoadingParent"] = "Loading parent directory...",
                ["StatusUploadCancelled"] = "Upload cancelled.",
                ["StatusUploading"] = "Uploading {0}...",
                ["StatusUploaded"] = "Uploaded {0}.",
                ["StatusUploadingFolder"] = "Uploading folder {0}...",
                ["StatusUploadedFolder"] = "Uploaded folder {0}.",
                ["ToastUploadSuccess"] = "Upload succeeded.",
                ["StatusDownloadCancelled"] = "Download cancelled.",
                ["StatusDownloading"] = "Downloading {0}...",
                ["StatusDownloadedFolder"] = "Downloaded folder {0}.",
                ["StatusDownloadedFile"] = "Downloaded {0}.",
                ["ToastDownloadSuccess"] = "Download succeeded.",
                ["StatusDeleting"] = "Deleting {0}...",
                ["StatusDeleted"] = "Deleted {0}.",
                ["StatusCreatingFolder"] = "Creating {0}...",
                ["StatusCreatedFolder"] = "Created folder {0}.",
                ["StatusLanguageChanged"] = "Language switched to {0}.",
                ["LogApplicationStarted"] = "Application started.",
                ["LogConnected"] = "Connected to {0}:{1}.",
                ["LogStartupPathFallback"] = "Startup path '{0}' was unavailable, switched to '{1}'.",
                ["LogConnectionFailed"] = "Connection failed: {0}",
                ["LogDisconnected"] = "Disconnected from the server.",
                ["LogRefreshed"] = "Refreshed {0}.",
                ["LogSelectedFile"] = "Selected file {0}.",
                ["LogEnteredDirectory"] = "Entered {0}.",
                ["LogMovedToParent"] = "Moved to parent directory {0}.",
                ["LogUploaded"] = "Uploaded {0} to {1}.",
                ["LogDownloadedDirectory"] = "Downloaded directory {0} to {1}.",
                ["LogDownloadedFile"] = "Downloaded {0} to {1}.",
                ["LogDeleted"] = "Deleted {0}.",
                ["LogCreatedDirectory"] = "Created directory {0}.",
                ["LogLanguageChanged"] = "Language switched to {0}.",
                ["DialogDownloadFolderTitle"] = "Choose a local folder for the download",
                ["DialogUploadFolderTitle"] = "Choose a local folder to upload",
                ["LogCategoryInfo"] = "INFO",
                ["LogCategoryConnect"] = "CONNECT",
                ["LogCategoryList"] = "LIST",
                ["LogCategoryUpload"] = "UPLOAD",
                ["LogCategoryDownload"] = "DOWNLOAD",
                ["LogCategoryDelete"] = "DELETE",
                ["LogCategoryCreate"] = "CREATE",
                ["LogCategorySelect"] = "SELECT",
                ["LogCategoryError"] = "ERROR",
                ["LogCategorySettings"] = "SETTINGS",
            },
            [AppLanguage.ChineseSimplified] = new Dictionary<string, string>
            {
                ["WorkspaceHeroTitle"] = "简洁、轻量、跨平台的 SFTP 工作区",
                ["SettingsHeroTitle"] = "配置应用偏好，并切换你习惯的界面语言",
                ["StatusLabel"] = "状态",
                ["WorkspaceNavButton"] = "工作区",
                ["SettingsNavButton"] = "设置",
                ["ConnectionMenu"] = "连接",
                ["FileMenu"] = "文件",
                ["BrowserMenu"] = "浏览",
                ["ViewMenu"] = "视图",
                ["CancelButton"] = "取消",
                ["ConnectionsTitle"] = "连接",
                ["NewConnectionButton"] = "新连接",
                ["NewConnectionTitle"] = "新连接",
                ["NewConnectionDescription"] = "保存服务器配置，并可在连接前先测试。",
                ["ConnectionNameLabel"] = "名称",
                ["TestConnectionButton"] = "测试",
                ["SaveConnectionButton"] = "保存",
                ["NoConnectionsHint"] = "还没有保存的连接。创建一个新连接后即可开始浏览。",
                ["TaskCenterTitle"] = "任务中心",
                ["TaskCenterEmpty"] = "任务",
                ["TaskCenterCount"] = "任务 ({0})",
                ["ConnectionTitle"] = "连接配置",
                ["HostLabel"] = "主机",
                ["PortLabel"] = "端口",
                ["UsernameLabel"] = "用户名",
                ["PasswordLabel"] = "密码",
                ["StartupPathLabel"] = "启动路径",
                ["HostPlaceholder"] = "sftp.example.com",
                ["UsernamePlaceholder"] = "用户名",
                ["PasswordPlaceholder"] = "密码",
                ["StartupPathPlaceholder"] = "/ 或相对路径",
                ["ConnectButton"] = "连接",
                ["DisconnectButton"] = "断开",
                ["ActionsTitle"] = "操作",
                ["UploadButton"] = "上传本地文件",
                ["UploadFolderButton"] = "上传本地文件夹",
                ["DownloadButton"] = "下载所选项",
                ["DeleteButton"] = "删除所选项",
                ["CreateFolderHint"] = "在当前目录创建文件夹",
                ["CreateFolderPlaceholder"] = "新建文件夹名称",
                ["CreateFolderButton"] = "创建文件夹",
                ["ExplorerTitle"] = "远程浏览",
                ["UpButton"] = "上一级",
                ["OpenButton"] = "打开",
                ["RefreshButton"] = "刷新",
                ["NameHeader"] = "名称",
                ["TypeHeader"] = "类型",
                ["SizeHeader"] = "大小",
                ["ModifiedHeader"] = "修改时间",
                ["ActivityTitle"] = "活动记录",
                ["SettingsTitle"] = "设置",
                ["SettingsDescription"] = "你可以在这里切换界面语言。设置会保存在本地，下次启动时自动恢复。",
                ["LanguageTitle"] = "语言",
                ["LanguageDescription"] = "选择应用界面的显示语言。",
                ["LanguageLabel"] = "显示语言",
                ["LanguagePreviewLabel"] = "当前语言",
                ["AppInfoTitle"] = "关于 PureSftp",
                ["AppInfoDescription"] = "PureSftp 专注于轻量级远程文件操作，用简洁的 Avalonia 桌面体验覆盖多个平台。",
                ["StateWorking"] = "处理中",
                ["StateConnected"] = "已连接",
                ["StateOffline"] = "未连接",
                ["ConnectionSummaryOffline"] = "一个轻量、跨平台、专注快速文件操作的 SFTP 工具。",
                ["ConnectionSummaryOnline"] = "{0}:{1}，用户 {2}",
                ["SelectionSummaryNone"] = "当前未选择任何项。",
                ["SelectionSummaryItem"] = "{0} · {1}",
                ["SelectionSummaryParent"] = "返回上一级目录。",
                ["LogSummaryNone"] = "暂无活动记录。",
                ["LogSummaryCount"] = "最近 {0} 条记录",
                ["DirectoryType"] = "文件夹",
                ["FileType"] = "文件",
                ["StatusDefault"] = "填写服务器信息后即可开始连接。",
                ["StatusConnecting"] = "正在连接服务器...",
                ["StatusConnected"] = "已连接到 {0}。",
                ["StatusTestingConnection"] = "正在测试...",
                ["StatusTestSucceeded"] = "测试成功。",
                ["StatusDisconnected"] = "连接已断开。",
                ["StatusDisconnecting"] = "正在断开连接...",
                ["StatusRefreshing"] = "正在刷新远程目录...",
                ["StatusLoaded"] = "已载入 {0}。",
                ["StatusFileReadyToDownload"] = "“{0}” 已准备好下载。",
                ["StatusOpening"] = "正在打开 {0}...",
                ["StatusOpened"] = "已打开 {0}。",
                ["StatusLoadingParent"] = "正在载入上级目录...",
                ["StatusUploadCancelled"] = "已取消上传。",
                ["StatusUploading"] = "正在上传 {0}...",
                ["StatusUploaded"] = "已上传 {0}。",
                ["StatusUploadingFolder"] = "正在上传文件夹 {0}...",
                ["StatusUploadedFolder"] = "已上传文件夹 {0}。",
                ["ToastUploadSuccess"] = "上传成功。",
                ["StatusDownloadCancelled"] = "已取消下载。",
                ["StatusDownloading"] = "正在下载 {0}...",
                ["StatusDownloadedFolder"] = "已下载文件夹 {0}。",
                ["StatusDownloadedFile"] = "已下载 {0}。",
                ["ToastDownloadSuccess"] = "下载成功。",
                ["StatusDeleting"] = "正在删除 {0}...",
                ["StatusDeleted"] = "已删除 {0}。",
                ["StatusCreatingFolder"] = "正在创建 {0}...",
                ["StatusCreatedFolder"] = "已创建文件夹 {0}。",
                ["StatusLanguageChanged"] = "界面语言已切换为 {0}。",
                ["LogApplicationStarted"] = "应用已启动。",
                ["LogConnected"] = "已连接到 {0}:{1}。",
                ["LogStartupPathFallback"] = "启动路径 “{0}” 不可用，已切换到 “{1}”。",
                ["LogConnectionFailed"] = "连接失败：{0}",
                ["LogDisconnected"] = "已断开服务器连接。",
                ["LogRefreshed"] = "已刷新 {0}。",
                ["LogSelectedFile"] = "已选择文件 {0}。",
                ["LogEnteredDirectory"] = "已进入 {0}。",
                ["LogMovedToParent"] = "已切换到上级目录 {0}。",
                ["LogUploaded"] = "已将 {0} 上传到 {1}。",
                ["LogDownloadedDirectory"] = "已将目录 {0} 下载到 {1}。",
                ["LogDownloadedFile"] = "已将 {0} 下载到 {1}。",
                ["LogDeleted"] = "已删除 {0}。",
                ["LogCreatedDirectory"] = "已创建目录 {0}。",
                ["LogLanguageChanged"] = "界面语言已切换为 {0}。",
                ["DialogDownloadFolderTitle"] = "选择下载到本地的目录",
                ["DialogUploadFolderTitle"] = "选择要上传的本地文件夹",
                ["LogCategoryInfo"] = "信息",
                ["LogCategoryConnect"] = "连接",
                ["LogCategoryList"] = "浏览",
                ["LogCategoryUpload"] = "上传",
                ["LogCategoryDownload"] = "下载",
                ["LogCategoryDelete"] = "删除",
                ["LogCategoryCreate"] = "创建",
                ["LogCategorySelect"] = "选择",
                ["LogCategoryError"] = "错误",
                ["LogCategorySettings"] = "设置",
            },
        };

    public LocalizationService(AppLanguage initialLanguage)
    {
        CurrentLanguage = initialLanguage;
        ApplyCulture(initialLanguage);
    }

    public AppLanguage CurrentLanguage { get; private set; }

    public event EventHandler? LanguageChanged;

    public void SetLanguage(AppLanguage language)
    {
        if (language == CurrentLanguage)
        {
            return;
        }

        CurrentLanguage = language;
        ApplyCulture(language);
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    public string Get(string key)
    {
        if (Resources[CurrentLanguage].TryGetValue(key, out var value))
        {
            return value;
        }

        if (Resources[AppLanguage.English].TryGetValue(key, out var fallbackValue))
        {
            return fallbackValue;
        }

        return key;
    }

    public string Get(string key, params object[] args)
    {
        return string.Format(CultureInfo.CurrentUICulture, Get(key), args);
    }

    private static void ApplyCulture(AppLanguage language)
    {
        var cultureName = language == AppLanguage.ChineseSimplified ? "zh-CN" : "en-US";
        var culture = new CultureInfo(cultureName);

        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }
}
