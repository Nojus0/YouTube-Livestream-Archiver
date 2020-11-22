﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Videos;

namespace Lite
{
    class Channel
    {
        public Channel(string Id, int minutesTimeout)
        {
            ChannelId = Id;
            MinutesTimeOut = minutesTimeout;
        }
        public string ChannelId { get; set; }
        public int MinutesTimeOut { get; set; }
    }

    class ConfigFile
    {
        public List<Channel> Channels = new List<Channel>();
    }
    public class Name
    {
        public static string Purify(string Input)
        {
            return Input.Replace('*', ' ').Replace('<', ' ').Replace('>', ' ').Replace(':', ' ').Replace('\"', ' ').Replace('\\', ' ').Replace('/', ' ').Replace('|', ' ').Replace('?', ' ');
        }
    }

    public class FilePaths
    {
        private static string ThumbnailFolder = $"{Directory.GetCurrentDirectory()}\\Thumbnails\\";
        private static string LivestreamsFolder = $"{Directory.GetCurrentDirectory()}\\Livestreams\\";
        private static string SecretsFolder = $"{Directory.GetCurrentDirectory()}\\Secrets\\";
        private static string ConfigFolder = $"{Directory.GetCurrentDirectory()}\\Config\\";

        public static string ConfigFile = $"{ConfigFolder}Config.json";
        public static string SecretsFile = $"{SecretsFolder}client_secrets.json";
        public static void Setup()
        {
            CheckFilesystem(new string[] { ThumbnailFolder, LivestreamsFolder, SecretsFolder, ConfigFolder}, true);
            CheckFilesystem(new string[] { SecretsFile }, false);

        }
        private static void CheckFilesystem(string[] Paths, bool IsDirectory)
        {
            if (IsDirectory)
                foreach (var Path in Paths)
                    if (!Directory.Exists(Path)) Directory.CreateDirectory(Path);

            if (!IsDirectory)
                foreach (var Path in Paths)
                    if (!File.Exists(Path)) File.Create(Path);
        }

        public static string GetThumbnailPath(string Filename)
        {
            return $"{ThumbnailFolder}{Filename}";
        }

        public static string GetLivestreamsPath(string Filename)
        {
            return $"{LivestreamsFolder}{Filename}";
        }
    }
    public class LivestreamObject
    {
        public LivestreamObject(Video info, string thumbnailPath, string livestreamPath)
        {
            Info = info;
            ThumbnailPath = thumbnailPath;
            LivestreamPath = livestreamPath;
        }

        public Video Info { get; set; }
        public string ThumbnailPath { get; set; }
        public string LivestreamPath { get; set; }
    }


    public class ActiveChannel
    {
        public ActiveChannel(string channelId, TimeSpan timeOut)
        {
            ChannelId = channelId;
            TimeOut = timeOut;
        }

        public string ChannelId { get; set; }
        public TimeSpan TimeOut { get; set; }

        internal async Task Sleep()
        {
            await Task.Delay(TimeOut);
        }

        public async Task Run()
        {
            while (true)
            {

                var Status = await Scrape.GetLivestreamStatusFromChannelId(ChannelId);

                if (!Status.IsLivestreaming)
                {
                    await Sleep();
                    continue;
                }

                if (Status.IsLivestreaming)
                {
                    var ytExplode = new YoutubeClient();

                    var metadata = await ytExplode.Videos.GetAsync(Status.videoId);
                    var StreamObject = new LivestreamObject(metadata, FilePaths.GetThumbnailPath(Name.Purify($"{metadata.Title} [{DateTime.Now.Ticks.GetHashCode()}].jpeg")), FilePaths.GetLivestreamsPath(Name.Purify($"{metadata.Title} [{DateTime.Now.Ticks.GetHashCode()}].mp4")));

                    new WebClient().DownloadFile(metadata.Thumbnails.MaxResUrl, StreamObject.ThumbnailPath);
                    await Streamlink.Download(StreamObject.LivestreamPath, metadata.Url);

                    var Upload = new Upload(FilePaths.SecretsFile);
                    await Upload.Init();
                    _ = Upload.CreateWithRetry(StreamObject, TimeSpan.FromHours(3));
                }

                await Sleep();
            }
        }


    }
}