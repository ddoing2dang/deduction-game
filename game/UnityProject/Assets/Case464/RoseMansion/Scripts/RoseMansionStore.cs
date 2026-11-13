using System;
using System.IO;
using Mystery.Interrogation;
using UnityEngine;

namespace Case464.RoseMansion
{
    // Uses the existing CaseProgressSave envelope; legacy dialogue saves remain separate.
    public sealed class RoseMansionStore
    {
        private readonly string directory=Path.Combine(Application.persistentDataPath,"Case464","RoseMansion");
        private string CheckpointPath=>Path.Combine(directory,"lobby-autosave.json");
        private string ArchivePath=>Path.Combine(directory,"ending-archive.json");
        public bool HasCheckpoint=>File.Exists(CheckpointPath);
        public static T Copy<T>(T value)=>JsonUtility.FromJson<T>(JsonUtility.ToJson(value));
        public CaseProgressSave ReadCheckpoint()=>Read<CaseProgressSave>(CheckpointPath);
        public RoseEndingArchive ReadArchive()=>File.Exists(ArchivePath)?Read<RoseEndingArchive>(ArchivePath):null;
        private static T Read<T>(string path)
        {
            var value=JsonUtility.FromJson<T>(File.ReadAllText(path));
            if(value==null)throw new InvalidDataException("저장 파일을 읽을 수 없습니다.");
            return value;
        }
        public void WriteCheckpoint(RoseProgress state)=>Write(CheckpointPath,new CaseProgressSave{schemaVersion=2,caseId=state.runId,theme=0,roseMansion=state});
        public void WriteArchive(RoseEndingArchive archive)=>Write(ArchivePath,archive);
        private void Write<T>(string path,T value)
        {
            Directory.CreateDirectory(directory);
            string temporary=path+"."+Guid.NewGuid().ToString("N")+".pending";
            File.WriteAllText(temporary,JsonUtility.ToJson(value,true));
            // Only this store's own checkpoint/archive is replaced; the old file is retained.
            if(File.Exists(path))File.Replace(temporary,path,path+".bak",true);
            else File.Move(temporary,path);
        }
    }
}
