using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

class IsoBuilder
{
    [STAThread]
    static void Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Usage: IsoBuilder <sourceDir> <outputIso> <volumeName>");
            return;
        }

        string sourceDir = Path.GetFullPath(args[0]);
        string outputIso = Path.GetFullPath(args[1]);
        string volumeName = args[2];

        try
        {
            Type fsType = Type.GetTypeFromProgID("IMAPI2FS.MsftFileSystemImage");
            if (fsType == null) { Console.WriteLine("IMAPI2FS not found"); return; }

            object fs = Activator.CreateInstance(fsType);
            SetProp(fs, fsType, "FreeMediaBlocks", 25000000);
            SetProp(fs, fsType, "VolumeName", volumeName);
            SetProp(fs, fsType, "FileSystemsToCreate", 4);

            Console.WriteLine("Adding files from: " + sourceDir);
            object root = GetProp(fs, fsType, "Root");
            Type rootType = root.GetType();
            Invoke(root, rootType, "AddTree", new object[] { sourceDir, false });

            Console.WriteLine("Creating ISO image...");
            object result = Invoke(fs, fsType, "CreateResultImage", new object[0]);
            Type resType = result.GetType();
            object comStream = GetProp(result, resType, "ImageStream");

            IStream stream = (IStream)comStream;

            Console.WriteLine("Writing ISO to: " + outputIso);
            byte[] buffer = new byte[1048576];
            long total = 0;

            IntPtr pcbRead = Marshal.AllocHGlobal(4);

            using (FileStream fstream = new FileStream(outputIso, FileMode.Create, FileAccess.Write))
            {
                while (true)
                {
                    stream.Read(buffer, buffer.Length, pcbRead);
                    int bytesRead = Marshal.ReadInt32(pcbRead);
                    if (bytesRead <= 0) break;
                    fstream.Write(buffer, 0, bytesRead);
                    total += bytesRead;
                    if (total % (100L * 1048576) < 524288)
                        Console.WriteLine("  Written: " + (total / 1048576) + " MB");
                }
            }

            Marshal.FreeHGlobal(pcbRead);

            Console.WriteLine("Done! ISO: " + outputIso);
            Console.WriteLine("Size: " + (new FileInfo(outputIso).Length / 1048576) + " MB");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
            if (ex.InnerException != null)
                Console.WriteLine("Inner: " + ex.InnerException.Message);
            Environment.Exit(1);
        }
    }

    static void SetProp(object obj, Type t, string name, object val)
    {
        t.InvokeMember(name, BindingFlags.SetProperty | BindingFlags.Public | BindingFlags.Instance,
            null, obj, new object[] { val });
    }

    static object GetProp(object obj, Type t, string name)
    {
        return t.InvokeMember(name, BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance,
            null, obj, null);
    }

    static object Invoke(object obj, Type t, string name, object[] args)
    {
        return t.InvokeMember(name, BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance,
            null, obj, args);
    }
}