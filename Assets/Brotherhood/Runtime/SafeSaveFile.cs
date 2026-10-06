using System;
using System.IO;
using System.Text;
namespace Brotherhood
{
    // Shared by the game and slot previews; all paths are supplied by callers.
    public static class SafeSaveFile
    {
        public static bool TryRead(string path,Func<string,bool> validate,out string json,out bool recovered)
        {
            json=null;recovered=false;
            foreach(string candidate in new[]{path,path+".bak",path+".tmp"})
            {
                try
                {
                    if(!File.Exists(candidate)||new FileInfo(candidate).Length>8*1024*1024)continue;
                    string text=File.ReadAllText(candidate);
                    if(!validate(text))continue;
                    json=text;recovered=candidate!=path;return true;
                }
                catch(Exception e) when(e is IOException||e is UnauthorizedAccessException||e is ArgumentException){ }
            }
            return false;
        }
        public static void Write(string path,string json,Func<string,bool> validate)
        {
            if(!validate(json))throw new InvalidDataException("Invalid save payload");
            string temporary=path+".tmp",backup=path+".bak";
            byte[] bytes=new UTF8Encoding(false).GetBytes(json);
            using(var stream=new FileStream(temporary,FileMode.Create,FileAccess.Write,FileShare.None))
            {stream.Write(bytes,0,bytes.Length);stream.Flush(true);}
            if(!File.Exists(path)){File.Move(temporary,path);return;}
            bool validPrimary=false;
            try{validPrimary=validate(File.ReadAllText(path));}catch(IOException){ }
            try{File.Replace(temporary,path,validPrimary?backup:null);}
            catch(Exception e) when(e is PlatformNotSupportedException||e is IOException)
            {
                // Retain a readable backup on filesystems without replace. If
                // interrupted, TryRead also accepts the flushed temporary file.
                if(validPrimary)File.Copy(path,backup,true);
                File.Delete(path);File.Move(temporary,path);
            }
        }
    }
}
