using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MediaPlaybackLib
{
    /// <summary>
    /// Contains all known Windows Media Player error codes.
    /// </summary>
    public static class WmpErrorCodes
    {
        // Unexpected failure from the Windows Media Player (see http://social.msdn.microsoft.com/Forums/fi-FI/wpf/thread/a7ef9747-8674-412b-8909-79b595309c55)
        // SF-2012-04-10: I've seen this when you mess with Position/SpeedRation when the player's DownloadProgress < 1. Silly.
        // see http://social.msdn.microsoft.com/Forums/en/wpf/thread/29f80c22-64a9-4dbb-8930-be6399bc1f58
        public const uint UnexpectedFailure = 0x8898050C;

        // Seen right after above...not very catastrophic though since the element keeps working if you let it.
        public const uint CatastrophicFailure = 0x8000FFFF;

        // From [MS-ERREF] @ http://msdn.microsoft.com/en-us/library/cc231196(PROT.10).aspx

        //Windows Media Player cannot open the specified URL. Verify that the Player is configured to use all available protocols, and then try again. 
        public const uint NS_E_WMP_PROTOCOL_PROBLEM = 0xC00D1194;

        //Windows Media Player cannot perform the requested action because there is not enough storage space on your computer. Delete some unneeded files on your hard disk, and then try again. 
        public const uint NS_E_WMP_NO_DISK_SPACE = 0xC00D1195;

        //The server denied access to the file. Verify that you are using the correct user name and password. 
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "LOGON")]
        public const uint NS_E_WMP_LOGON_FAILURE = 0xC00D1196;

        //Windows Media Player cannot find the file. If you are trying to play, burn, or sync an item that is in your library, the item might point to a file that has been moved, renamed, or deleted. 
        public const uint NS_E_WMP_CANNOT_FIND_FILE = 0xC00D1197;

        //Windows Media Player cannot connect to the server. The server name might not be  correct, the server might not be available, or your proxy settings might not be correct. 
        public const uint NS_E_WMP_SERVER_INACCESSIBLE = 0xC00D1198;

        //Windows Media Player cannot play the file. The Player might not support the file type or might not support the codec that was used to compress the file. 
        public const uint NS_E_WMP_UNSUPPORTED_FORMAT = 0xC00D1199;

        //Windows Media Player cannot play the file. The Player might not support the file type or a required codec might not be installed on your computer. 
        public const uint NS_E_WMP_DSHOW_UNSUPPORTED_FORMAT = 0xC00D119A;

        //Windows Media Player cannot create the playlist because the name already exists. Type a different playlist name.  
        public const uint NS_E_WMP_PLAYLIST_EXISTS = 0xC00D119B;

        //Windows Media Player cannot delete the playlist because it contains items that are not digital media files. Any digital media files in the playlist were deleted.  
        public const uint NS_E_WMP_NONMEDIA_FILES = 0xC00D119C;

        //The playlist cannot be opened because it is stored in a shared folder on another computer. If possible, move the playlist to the playlists folder on your computer. 
        public const uint NS_E_WMP_INVALID_ASX = 0xC00D119D;

        //Windows Media Player is already in use. Stop playing any items, close all Player dialog boxes, and then try again. 
        public const uint NS_E_WMP_ALREADY_IN_USE = 0xC00D119E;

        //Windows Media Player encountered an error while burning. Verify that the burner is connected properly and that the disc is clean and not damaged. 
        public const uint NS_E_WMP_IMAPI_FAILURE = 0xC00D119F;

        //Windows Media Player has encountered an unknown error with your portable device. Reconnect your portable device, and then try again.  
        public const uint NS_E_WMP_WMDM_FAILURE = 0xC00D11A0;

        //A codec is required to play this file. To determine if this codec  is available to download from the Web, click Web Help. 
        public const uint NS_E_WMP_CODEC_NEEDED_WITH_4CC = 0xC00D11A1;

        //An audio codec is needed to play this file. To determine if this codec is available to download from the Web, click Web Help.  
        public const uint NS_E_WMP_CODEC_NEEDED_WITH_FORMATTAG = 0xC00D11A2;

        //To play the file, you must install the latest Windows service pack. To install the service pack from the Windows Update Web site, click Web Help.  
        public const uint NS_E_WMP_MSSAP_NOT_AVAILABLE = 0xC00D11A3;

        //Windows Media Player no longer detects a portable device. Reconnect your portable device, and then try again.  
        public const uint NS_E_WMP_WMDM_INTERFACEDEAD = 0xC00D11A4;

        //Windows Media Player cannot sync the file because the portable device does not support protected files.  
        public const uint NS_E_WMP_WMDM_NOTCERTIFIED = 0xC00D11A5;

        //This file does not have sync rights. If you obtained this file from an online store, go to the online store to get sync rights.  
        public const uint NS_E_WMP_WMDM_LICENSE_NOTEXIST = 0xC00D11A6;

        //Windows Media Player cannot sync the file because the sync rights have expired. Go to the content provider's online store to get new sync rights.  
        public const uint NS_E_WMP_WMDM_LICENSE_EXPIRED = 0xC00D11A7;

        //The portable device is already in use. Wait until the current task finishes or quit other programs that might be using the portable device, and then try again. 
        public const uint NS_E_WMP_WMDM_BUSY = 0xC00D11A8;

        //Windows Media Player cannot sync the file because the content provider or device prohibits it. You might be able to resolve this problem by going to the content provider's online store to get sync rights. 
        public const uint NS_E_WMP_WMDM_NORIGHTS = 0xC00D11A9;

        //The content provider has not granted you the right to sync this file. Go to the content provider's online store to get  sync rights.  
        public const uint NS_E_WMP_WMDM_INCORRECT_RIGHTS = 0xC00D11AA;

        //Windows Media Player cannot burn the files to the CD. Verify that the disc is clean and not damaged. If necessary, select a slower recording speed or try a different brand of blank discs. 
        public const uint NS_E_WMP_IMAPI_GENERIC = 0xC00D11AB;

        //Windows Media Player cannot burn the files. Verify that the burner is connected properly, and then try again.  
        public const uint NS_E_WMP_IMAPI_DEVICE_NOTPRESENT = 0xC00D11AD;

        //Windows Media Player cannot burn the files. Verify that the burner is connected properly and that the disc is clean and not damaged. If the burner is already in use, wait until the current task finishes or quit other programs that might be using the burner.  
        public const uint NS_E_WMP_IMAPI_DEVICE_BUSY = 0xC00D11AE;

        //Windows Media Player cannot burn the files to the CD.  
        public const uint NS_E_WMP_IMAPI_LOSS_OF_STREAMING = 0xC00D11AF;

        //Windows Media Player cannot play the file. The server might not be available or there might be a problem with your network or firewall settings.  
        public const uint NS_E_WMP_SERVER_UNAVAILABLE = 0xC00D11B0;

        //Windows Media Player encountered a problem while playing the file. For additional assistance, click Web Help.  
        public const uint NS_E_WMP_FILE_OPEN_FAILED = 0xC00D11B1;

        //Windows Media Player must connect to the Internet to verify the file's media usage rights. Connect to the Internet, and then try again.  
        public const uint NS_E_WMP_VERIFY_ONLINE = 0xC00D11B2;

        //Windows Media Player cannot play the file because a network error occurred. The server might not be available. Verify that you are connected to the network and that your proxy settings are correct.  
        public const uint NS_E_WMP_SERVER_NOT_RESPONDING = 0xC00D11B3;

        //Windows Media Player cannot restore your media usage rights because it could not find any backed up rights on your computer.  
        public const uint NS_E_WMP_DRM_CORRUPT_BACKUP = 0xC00D11B4;

        //Windows Media Player cannot download media usage rights because the server is not available (for example, the server might be busy or not online).  
        public const uint NS_E_WMP_DRM_LICENSE_SERVER_UNAVAILABLE = 0xC00D11B5;

        //Windows Media Player cannot play the file. A network firewall might be preventing the Player from opening the file by using the UDP transport protocol. If you typed a URL in the Open URL dialog box, try using a different transport protocol (for example, "http:").  
        public const uint NS_E_WMP_NETWORK_FIREWALL = 0xC00D11B6;

        //Insert the removable media, and then try again. 
        public const uint NS_E_WMP_NO_REMOVABLE_MEDIA = 0xC00D11B7;

        //Windows Media Player cannot play the file because the proxy server is not responding. The proxy server might be temporarily unavailable or your Player proxy settings might not be valid.  
        public const uint NS_E_WMP_PROXY_CONNECT_TIMEOUT = 0xC00D11B8;

        //To play the file, you might need to install a later version of Windows Media Player. On the Help menu, click Check for Updates, and then follow the instructions. For additional assistance, click Web Help.  
        public const uint NS_E_WMP_NEED_UPGRADE = 0xC00D11B9;

        //Windows Media Player cannot play the file because there is a problem with your sound device. There might not be a sound device installed on your computer, it might be in use by another program, or it might not be functioning properly.  
        public const uint NS_E_WMP_AUDIO_HW_PROBLEM = 0xC00D11BA;

        //Windows Media Player cannot play the file because the specified protocol is not supported. If you typed a URL in the Open URL dialog box, try using a different transport protocol (for example, "http:" or "rtsp:").   
        public const uint NS_E_WMP_INVALID_PROTOCOL = 0xC00D11BB;

        //Windows Media Player cannot add the file to the library because the file format is not supported.  
        public const uint NS_E_WMP_INVALID_LIBRARY_ADD = 0xC00D11BC;

        //Windows Media Player cannot play the file because the specified protocol is not supported. If you typed a URL in the Open URL dialog box, try using a different transport protocol (for example, "mms:").  
        public const uint NS_E_WMP_MMS_NOT_SUPPORTED = 0xC00D11BD;

        //Windows Media Player cannot play the file because there are no streaming protocols selected. Select one or more protocols, and then try again.  
        public const uint NS_E_WMP_NO_PROTOCOLS_SELECTED = 0xC00D11BE;

        //Windows Media Player cannot switch to Full Screen. You might need to adjust your Windows display settings. Open display settings in Control Panel, and then try setting Hardware acceleration to Full.  
        public const uint NS_E_WMP_GOFULLSCREEN_FAILED = 0xC00D11BF;

        //Windows Media Player cannot play the file because a network error occurred. The server might not be available (for example, the server is busy or not online) or you might not be connected to the network.  
        public const uint NS_E_WMP_NETWORK_ERROR = 0xC00D11C0;

        //Windows Media Player cannot play the file because the server is not responding. Verify that you are connected to the network, and then try again later.  
        public const uint NS_E_WMP_CONNECT_TIMEOUT = 0xC00D11C1;

        //Windows Media Player cannot play the file because the multicast protocol is not enabled. On the Tools menu, click Options, click the Network tab, and then select the Multicast check box. For additional assistance, click Web Help. 
        public const uint NS_E_WMP_MULTICAST_DISABLED = 0xC00D11C2;

        //Windows Media Player cannot play the file because a network problem occurred. Verify that you are connected to the network, and then try again later. 
        public const uint NS_E_WMP_SERVER_DNS_TIMEOUT = 0xC00D11C3;

        //Windows Media Player cannot play the file because the network proxy server cannot be found. Verify that your proxy settings are correct, and then try again. 
        public const uint NS_E_WMP_PROXY_NOT_FOUND = 0xC00D11C4;

        //Windows Media Player cannot play the file because it is corrupted. 
        public const uint NS_E_WMP_TAMPERED_CONTENT = 0xC00D11C5;

        //Your computer is running low on memory. Quit other programs, and then try again. 
        public const uint NS_E_WMP_OUTOFMEMORY = 0xC00D11C6;

        //Windows Media Player cannot play, burn, rip, or sync the file because a required audio codec is not installed on your computer. 
        public const uint NS_E_WMP_AUDIO_CODEC_NOT_INSTALLED = 0xC00D11C7;

        //Windows Media Player cannot play the file because the required video codec is not installed on your computer.  
        public const uint NS_E_WMP_VIDEO_CODEC_NOT_INSTALLED = 0xC00D11C8;

        //Windows Media Player cannot burn the files. If the burner is busy, wait for the current task to finish. If necessary, verify that the burner is connected properly and that you have installed the latest device driver.  
        public const uint NS_E_WMP_IMAPI_DEVICE_INVALIDTYPE = 0xC00D11C9;

        //Windows Media Player cannot play the protected file because there is a problem with your sound device. Try installing a new device driver or use a different sound device.  
        public const uint NS_E_WMP_DRM_DRIVER_AUTH_FAILURE = 0xC00D11CA;

        //Windows Media Player encountered a network error. Restart the Player. 
        public const uint NS_E_WMP_NETWORK_RESOURCE_FAILURE = 0xC00D11CB;

        //Windows Media Player is not installed properly. Reinstall the Player.  
        public const uint NS_E_WMP_UPGRADE_APPLICATION = 0xC00D11CC;

        //Windows Media Player encountered an unknown error. For additional assistance, click Web Help.  
        public const uint NS_E_WMP_UNKNOWN_ERROR = 0xC00D11CD;

        //Windows Media Player cannot play the file because the required codec is not valid.  
        public const uint NS_E_WMP_INVALID_KEY = 0xC00D11CE;

        //The CD drive is in use by another user. Wait for the task to complete, and then try again.  
        public const uint NS_E_WMP_CD_ANOTHER_USER = 0xC00D11CF;

        //Windows Media Player cannot play, sync, or burn the protected file because a problem occurred with the Windows Media Digital Rights Management (DRM) system. You might need to connect to the Internet to update your DRM components. For additional assistance, click Web Help. 
        public const uint NS_E_WMP_DRM_NEEDS_AUTHORIZATION = 0xC00D11D0;

        //Windows Media Player cannot play the file because there might be a problem with your sound or video device. Try installing an updated device driver.  
        public const uint NS_E_WMP_BAD_DRIVER = 0xC00D11D1;

        //Windows Media Player cannot access the file. The file might be in use, you might not have access to the computer where the file is stored, or your proxy settings might not be correct.  
        public const uint NS_E_WMP_ACCESS_DENIED = 0xC00D11D2;

        //The content provider prohibits this action. Go to the content provider's online store to get new media usage rights. 
        public const uint NS_E_WMP_LICENSE_RESTRICTS = 0xC00D11D3;

        //Windows Media Player cannot perform the requested action at this time.  
        public const uint NS_E_WMP_INVALID_REQUEST = 0xC00D11D4;

        //Windows Media Player cannot burn the files because there is not enough free disk space to store the temporary files. Delete some unneeded files on your hard disk, and then try again. 
        public const uint NS_E_WMP_CD_STASH_NO_SPACE = 0xC00D11D5;

        //Your media usage rights have become corrupted or are no longer valid. This might happen if you have replaced hardware components in your computer. 
        public const uint NS_E_WMP_DRM_NEW_HARDWARE = 0xC00D11D6;

        //The required Windows Media Digital Rights Management (DRM) component cannot be validated. You might be able resolve the problem by reinstalling the Player.  
        public const uint NS_E_WMP_DRM_INVALID_SIG = 0xC00D11D7;

        //You have exceeded your restore limit for the day. Try restoring your media usage rights tomorrow. 
        public const uint NS_E_WMP_DRM_CANNOT_RESTORE = 0xC00D11D8;

        //Some files might not fit on the CD. The required space cannot be calculated accurately because some files might be missing duration information. To ensure the calculation is accurate, play the files that are missing duration information. 
        public const uint NS_E_WMP_BURN_DISC_OVERFLOW = 0xC00D11D9;

        //Windows Media Player cannot verify the file's media usage rights. If you obtained this file from an online store, go to the online store to get the necessary rights.  
        public const uint NS_E_WMP_DRM_GENERIC_LICENSE_FAILURE = 0xC00D11DA;

        //It is not possible to sync because this device's internal clock is not set correctly. To set the clock, select the option to set the device clock on the Privacy tab of the Options dialog box, connect to the Internet, and then sync the device again. For additional assistance, click Web Help.  
        public const uint NS_E_WMP_DRM_NO_SECURE_CLOCK = 0xC00D11DB;

        //Windows Media Player cannot play, burn, rip, or sync the protected file because you do not have the appropriate rights.  
        public const uint NS_E_WMP_DRM_NO_RIGHTS = 0xC00D11DC;

        //Windows Media Player encountered an error during upgrade. 
        public const uint NS_E_WMP_DRM_INDIV_FAILED = 0xC00D11DD;

        //Windows Media Player cannot connect to the server because it is not accepting any new connections. This could be because it has reached its maximum connection limit. Please try again later. 
        public const uint NS_E_WMP_SERVER_NONEWCONNECTIONS = 0xC00D11DE;

        //A number of queued files cannot be played. To find information about the problem, click the Now Playing tab, and then click the icon next to each file in the List pane. 
        public const uint NS_E_WMP_MULTIPLE_ERROR_IN_PLAYLIST = 0xC00D11DF;

        //Windows Media Player encountered an error while erasing the rewritable CD or DVD. Verify that the CD or DVD burner is connected properly and that the disc is clean and not damaged.  
        public const uint NS_E_WMP_IMAPI2_ERASE_FAIL = 0xC00D11E0;

        //Windows Media Player cannot erase the rewritable CD or DVD. Verify that the CD or DVD burner is connected properly and that the disc is clean and not damaged. If the burner is already in use, wait until the current task finishes or quit other programs that might be using the burner.  
        public const uint NS_E_WMP_IMAPI2_ERASE_DEVICE_BUSY = 0xC00D11E1;

        //A Windows Media Digital Rights Management (DRM) component encountered a problem. If you are trying to use a file that you obtained from an online store, try going to the online store and getting the appropriate usage rights.  
        public const uint NS_E_WMP_DRM_COMPONENT_FAILURE = 0xC00D11E2;

        //It is not possible to obtain device's certificate. Please contact the device manufacturer for a firmware update or for other steps to resolve this problem.  
        public const uint NS_E_WMP_DRM_NO_DEVICE_CERT = 0xC00D11E3;

        //Windows Media Player encountered an error when connecting to the server. The security information from the server could not be validated. 
        public const uint NS_E_WMP_SERVER_SECURITY_ERROR = 0xC00D11E4;

        //An audio device was disconnected or reconfigured. Verify that the audio device is connected, and then try to play the item again.  
        public const uint NS_E_WMP_AUDIO_DEVICE_LOST = 0xC00D11E5;

        //Windows Media Player could not complete burning because the disc is not compatible with your drive. Try inserting a different kind of recordable media or use a disc that supports a write speed that is compatible with your drive. 
        public const uint NS_E_WMP_IMAPI_MEDIA_INCOMPATIBLE = 0xC00D11E6;
    }
}
