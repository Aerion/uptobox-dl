using System;
using System.Web;

namespace UptoboxDl.UptoboxClient;

public static class LinkParser
{
    public enum LinkType
    {
        Unknown,
        DirectLink,
        Folder,
    }

    public static LinkType GetLinkType(string link)
    {
        // Simple implementation: if the url contains
        if (!Uri.TryCreate(link, UriKind.Absolute, out var url))
        {
            return LinkType.Unknown;
        }

        if (url.AbsolutePath == "/user_public")
        {
            return LinkType.Folder;
        }

        return LinkType.DirectLink;
    }

    public static string ParseFileCodeFromDirectLink(string link)
    {
        var url = new Uri(link, UriKind.Absolute);
        // Simple implementation, let's assume that every link has its filecode as its path
        return url.AbsolutePath[1..];
    }

    public static (string folder, string hash) ParseFolderHashFromLink(string link)
    {
        var url = new Uri(link, UriKind.Absolute);
        var queryParams = HttpUtility.ParseQueryString(url.Query);

        return (queryParams["folder"], queryParams["hash"]);
    }
}