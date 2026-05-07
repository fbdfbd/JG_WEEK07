using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public static class GameplayAnalyticsGoogleUploader
{
    private const string UploadUrl =
    "https://script.google.com/macros/s/AKfycbyaYMMmOLmvkAiD3P7JAvP2_GJirEYTeLzxV2lAtQjw_60oP3aIpcWwDFp5EIzatufK/exec?token=0944";
    public static async Task<bool> Upload(string path, string sessionId)
    {
        string json = JsonUtility.ToJson(new UploadPayload
        {
            sessionId = sessionId,
            fileName = Path.GetFileName(path),
            payload = File.ReadAllText(path)
        });

        using UnityWebRequest request = new(UploadUrl, UnityWebRequest.kHttpVerbPOST);
        byte[] body = Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        Debug.Log($"Analytics upload start: {path}");

        UnityWebRequestAsyncOperation operation = request.SendWebRequest();

        while (!operation.isDone)
        {
            await Task.Yield();
        }

        Debug.Log(
            $"Analytics upload result: {request.result}, " +
            $"code: {request.responseCode}, " +
            $"body: {request.downloadHandler.text}");

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Analytics upload failed: {request.error}");
            return false;
        }

        return true;

    }

    [System.Serializable]
    private class UploadPayload
    {
        public string sessionId;
        public string fileName;
        public string payload;
    }
}
