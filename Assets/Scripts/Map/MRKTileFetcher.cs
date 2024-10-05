﻿#define MRK_PROFILE

using System.Collections;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace MRK
{
    public abstract class MRKTileFetcher
    {
        public abstract IEnumerator Fetch(MRKTileFetcherContext context, string tileSet, MRKTileID id, Reference<UnityWebRequest> request, bool low = false);
    }

    public class MRKTileFetcherContext
    {
        public bool Error;
        public Texture2D Texture;
        public byte[] Data;
        public readonly MRKSelfContainedPtr<MRKMonitoredTexture> MonitoredTexture;

        public MRKTileFetcherContext()
        {
            MonitoredTexture = new MRKSelfContainedPtr<MRKMonitoredTexture>(() => new MRKMonitoredTexture(Texture));
        }
    }

    public class MRKFileTileFetcher : MRKTileFetcher
    {
        public string GetFolderPath(string tileSet)
        {
            return $"{Application.persistentDataPath}{Path.DirectorySeparatorChar}Tiles{Path.DirectorySeparatorChar}{tileSet}";
        }

        public bool Exists(string tileSet, MRKTileID id, bool low = false)
        {
            string lowPrefix = low ? "low_" : "";
            return File.Exists($"{GetFolderPath(tileSet)}{Path.DirectorySeparatorChar}{lowPrefix}{id.GetHashCode()}.png");
        }

        public override IEnumerator Fetch(MRKTileFetcherContext context, string tileSet, MRKTileID id, Reference<UnityWebRequest> request, bool low = false)
        {
            string dir = GetFolderPath(tileSet);
            if (!Directory.Exists(dir))
            {
                context.Error = true;
                yield break;
            }

            string lowPrefix = low ? "low_" : "";
            string path = $"{dir}{Path.DirectorySeparatorChar}{lowPrefix}{id.GetHashCode()}.png";
            if (!File.Exists(path))
            {
                context.Error = true;
                yield break;
            }

            UnityWebRequest req = UnityWebRequestTexture.GetTexture($"file:///{path}", true);
            request.Value = req;
            req.SendWebRequest();

            while (!req.isDone)
            {
                yield return new WaitForSeconds(0.2f);
            }

            if (req.result != UnityWebRequest.Result.Success)
            {
                context.Data = req.downloadHandler.data;
                context.Error = true;
                Debug.Log(req.error + req.downloadHandler.error);
                yield break;
            }

            context.Texture = DownloadHandlerTexture.GetContent(req);
            //req.downloadHandler.Dispose();
        }

        public async Task SaveToDisk(string tileset, MRKTileID id, byte[] tex, bool low, CancellationToken cancellationToken = default)
        {
            string dir = GetFolderPath(tileset);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string lowPrefix = low ? "low_" : "";
            string path = $"{dir}{Path.DirectorySeparatorChar}{lowPrefix}{id.GetHashCode()}.png";

            using (FileStream fs = File.OpenWrite(path))
            {
                await fs.WriteAsync(tex, 0, tex.Length, cancellationToken);
            }
        }
    }

    public class MRKRemoteTileFetcher : MRKTileFetcher
    {
        public override IEnumerator Fetch(MRKTileFetcherContext context, string tileSet, MRKTileID id, Reference<UnityWebRequest> request, bool low = false)
        {
        __start:
            MRKTilesetProvider provider = MRKTileRequestor.Instance.GetTilesetProvider(tileSet);
            string path = string.Format(provider.API, id.Z, id.X, id.Y).Replace("-", "%2D");
            if (low)
            {
                //lower res
                path = path.Replace("@2x", "");
                //.Replace("/512/", "/256/");
            }

            UnityWebRequest req = UnityWebRequestTexture.GetTexture(path, false);
            request.Value = req;
            req.SendWebRequest();

            while (!req.isDone)
            {
                yield return new WaitForSeconds(0.2f);
            }

            if (req.result != UnityWebRequest.Result.Success)
            {
                context.Error = true;
                Debug.Log(req.error + req.downloadHandler.error);
                yield break;
            }

            context.Texture = DownloadHandlerTexture.GetContent(req);
            context.Data = req.downloadHandler.data;

            if (context.Texture == null) goto __start;
        }
    }
}
