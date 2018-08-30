using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace CleanUpFolder
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var credentials = new NetworkCredential(args[1], args[2]);
                DeleteFTPDirectory(args[0], credentials);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception happen. Message: " + ex.Message + " .Stack Trace: " + ex.StackTrace);
            }

            Console.WriteLine("Cleaning up the FTP Folder completed!");
        }

        private static void DeleteFTPDirectory(string url, NetworkCredential credentials)
        {
            if (!url.EndsWith("/"))
            {
                url += "/";
            }

            var lines = ListDirectoryDetails(url, credentials);

            foreach (var line in lines)
            {
                var permissions = GetFileUrlAndTypeOfItems(url, line, out var fileUrl);

                if (IsDirectory(permissions))
                {
                    DeleteFTPDirectory(fileUrl + "/", credentials);
                }
                else
                {
                    DeleteFile(credentials, fileUrl);
                }
            }
            
            RemoveEmptyDirectory(url, credentials);
        }

        private static bool IsDirectory(string permissions)
        {
            if (string.IsNullOrEmpty(permissions) || permissions.Length < 2)
            {
                return false;
            }

            return permissions.ToLower()[1] == 'd';
        }

        private static string GetFileUrlAndTypeOfItems(string url, string line, out string fileUrl)
        {
            string[] tokens =
                line.Split(new[] {' '}, 9, StringSplitOptions.RemoveEmptyEntries);
            var name = string.Empty;
            for (var i = 3; i <= tokens.Length - 1; i++)
            {
                name += tokens[i] + " ";
            }
            
            name = name.TrimEnd();

            var permissions = tokens[2];
            fileUrl = url + name;
            return permissions;
        }

        private static List<string> ListDirectoryDetails(string url, NetworkCredential credentials)
        {
            var directoryListRequest = (FtpWebRequest) WebRequest.Create(url);
            directoryListRequest.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
            directoryListRequest.Credentials = credentials;
            directoryListRequest.Timeout = 360000;
            directoryListRequest.ReadWriteTimeout = 360000;
            var lines = new List<string>();

            using (var listResponse = (FtpWebResponse) directoryListRequest.GetResponse())
            using (var listStream = listResponse.GetResponseStream())
            {
                if (listStream == null)
                {
                    throw new ArgumentNullException($"The {url} does not existed or wrong path", nameof(listStream));
                }

                using (var listReader = new StreamReader(listStream))
                {
                    while (!listReader.EndOfStream)
                    {
                        lines.Add(listReader.ReadLine());
                    }
                }
            }

            return lines;
        }

        private static void DeleteFile(NetworkCredential credentials, string fileUrl)
        {
            Console.WriteLine("Deleting file: " + fileUrl);
            var deleteRequest = (FtpWebRequest)WebRequest.Create(fileUrl);
            deleteRequest.Method = WebRequestMethods.Ftp.DeleteFile;
            deleteRequest.Credentials = credentials;
            deleteRequest.GetResponse();
            Console.WriteLine("Complete delete file: " + fileUrl);
        }

        private static void RemoveEmptyDirectory(string url, NetworkCredential credentials)
        {
            Console.WriteLine("Deleting folder: " + url);
            var removeRequest = (FtpWebRequest)WebRequest.Create(url);
            removeRequest.Method = WebRequestMethods.Ftp.RemoveDirectory;
            removeRequest.Credentials = credentials;

            removeRequest.GetResponse();
            Console.WriteLine("Complete deleting folder: " + url);
        }
    }
}
