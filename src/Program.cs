using System;
using System.IO;

using NitroMdlConv.Common;

namespace NitroMdlConv
{
    using FlErr = CMdlFile.MdlFileError;

    class Program
    {
        static void Main(string[] args)
        {
            CMdlFile file = new CMdlFile();
            
            if (args.Length < 1)
            {
                Console.WriteLine("No source path defined");
                return;
            }
            
            Entities modelData = new Entities();
            for (int an=0; an<args.Length; ++an)
            {
                Entities parsedData = new Entities();
                FlErr result = file.LoadFile(path: args[an]);
                LogFileError(result, args[an]);
                if (FlErr.NoError != result)
                {
                    continue;
                }
                
                if (file.IsMorph)  // TODO: Read and parse a morphlist file instead of auto-detection/mapping
                {
                    parsedData.meshes = modelData.meshes;  // temporary shallow copy
                }
                if (CFileParser.TryParse(file, ref parsedData))
                {
                    if (modelData.IsDefined())
                    {// succeeding pass, copy new nodes and mesh data
                        // Integrate nodes
                        if ((null != parsedData.rootNode) && parsedData.rootNode.HasChilds())
                        {
                            if (!file.IsMorph)
                            {// skip first
                                var nodes = parsedData.rootNode.Childs;
                                modelData.rootNode.Childs.AddRange(nodes.GetRange(1, nodes.Count - 1));
                            //} else {
                                //modelData.rootNode.Childs.AddRange(parsedData.rootNode.Childs);
                            }
                        }
                        // Integrate meshes
                        if ((null != parsedData.meshes) && (parsedData.meshes.Count > 0))
                        {
                            modelData.meshes.AddRange(parsedData.meshes);
                        }
                    } else {  // first or previously incomplete pass
                        modelData.rootNode = parsedData.rootNode;
                        modelData.meshes = parsedData.meshes;
                    }
                } else {
                    Console.WriteLine($"Parsing of model {file.Filename} interrupted. Left at byte: {file.Reader.ReaderPos}. Path: {args[an]}");
                }
            }// load each file

            if (!modelData.HasAny())
            {
                Console.WriteLine("Failed to load any model data, aborting...");
                return;
            }

            string filename = Path.GetFileNameWithoutExtension(args[0]);
            try {
                using (FileStream fs =
                    File.Create(
                        Path.Combine(
                            Path.GetDirectoryName(args[0]),
                            filename + ".dae"
                    )))
                {
                    var ColladaWriter = new CDaeSerializer();
                    ColladaWriter.RootName = filename;
                    ColladaWriter.Serialize(fs, modelData);
                }
            } catch (Exception ex){
                Console.WriteLine($"An exception occured in {ex.Source}: {ex.Message}");
                return;
            }
            Console.WriteLine("File successfully converted");
        }


        static void LogFileError(FlErr errorCode, string filePathArg)
        {
            switch (errorCode)
            {
                case FlErr.NoError:
                Console.WriteLine(String.Format("Load MDL-file {0}", Path.GetFileNameWithoutExtension(filePathArg)));
                    break;

                case FlErr.InvalidPathError:
                    Console.WriteLine("Invalid path argument: " + filePathArg);
                    break;

                case FlErr.NoFileError:
                    Console.WriteLine(String.Format("File {0} does not exist.", filePathArg));
                    break;

                case FlErr.InvalidDataError:
                    Console.WriteLine(String.Format("{0} is not a MDL-file", Path.GetFileName(filePathArg)));
                    break;

                case FlErr.EmptyFileError:
                    Console.WriteLine(String.Format("File {0} is empty.", Path.GetFileName(filePathArg)));
                    break;

                case FlErr.IncompatibleError:
                    Console.WriteLine(String.Format("{0} is not a compatible MDL-file", Path.GetFileNameWithoutExtension(filePathArg)));
                    break;

                case FlErr.NoContentError:
                    Console.WriteLine(String.Format("MDL-file {0} contains nothing", Path.GetFileNameWithoutExtension(filePathArg)));
                    break;

                case FlErr.DefaultError:
                    Console.WriteLine(String.Format("Failed to load file {0}", Path.GetFileName(filePathArg)));
                    break;

                default:
                    throw new NotImplementedException("Unknown CMdlFileError");
            }
        }
    }
}
