using System;
using System.IO;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dropbox.Api;
using Dropbox.Api.Files;

namespace Secure_Upload1
{
    class Program
    {
        static byte[] Key;
        static byte[] IV;
        
       
        static void Main(string[] args)
        {
            try {
                var task = Task.Run((Func<Task>)Secure_Upload1.Program.Run);
                task.Wait();
            }
            catch(AggregateException e)
            {
                Console.WriteLine(e.ToString());
                Console.Read();

            }


        }
        
        
        static async Task<byte []> encrypt(string data)
        {
           
            byte[] encrypted;
            using (Aes enc = Aes.Create())
            {
                Aes encaes = Aes.Create();
                encaes.Key = enc.Key;
                encaes.IV = enc.IV;

                Console.WriteLine("{0} {1}", Encoding.UTF8.GetString(encaes.Key), Encoding.UTF8.GetString(encaes.IV));
                Program.Key = enc.Key;
                Program.IV = enc.IV;
                enc.Padding =PaddingMode.None;
                
                
                                               
                ICryptoTransform encryptor = encaes.CreateEncryptor(encaes.Key, encaes.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(data);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }
            return encrypted;
        }

        async Task <String> Decrypt(byte[] data)
        {
            String decrypted;
            using (Aes dec = Aes.Create())
            {
                dec.Key = Program.Key;
                dec.IV = Program.IV;
                dec.Padding = PaddingMode.None;
                Console.WriteLine("{0} {1}", Encoding.UTF8.GetString(dec.Key), Encoding.UTF8.GetString(dec.IV));
                ICryptoTransform decryptor = dec.CreateDecryptor(dec.Key, dec.IV);
                using (MemoryStream msDecrypt = new MemoryStream(data))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            decrypted = srDecrypt.ReadToEnd();

                            
                        }
                    }
                    
                       
                   

                }
            }
            return decrypted;
        }



        static async Task Run()
        {
            var dbx = new DropboxClient("Access Token");
            var det = await dbx.Users.GetCurrentAccountAsync();

            Console.WriteLine("Test");
            Console.WriteLine("{0} - {1}", det.AccountId, det.Email);
            
            Program p = new Program();
            await p.ListFolder(dbx);
            
            await p.Upload(dbx, @"D:\input.txt", "test", "input.txt");
            await p.Download(dbx, "test", "input.txt");
            Console.Read();

        }
         async Task ListFolder(DropboxClient dbx)
        {
            var list = await dbx.Files.ListFolderAsync(string.Empty);

            foreach (var folder in list.Entries.Where(i => i.IsFolder))
            {
                Console.WriteLine("D {0}", folder.Name);
                Console.Read();

            }
            foreach (var files in list.Entries.Where(i => i.IsFile))
            {
                Console.WriteLine("F {0}", files.Name);
            }
            
        }
        async Task Download(DropboxClient dbx, String folder, String filename)
        {
            Console.WriteLine(dbx.Users);
            String stream_data;

            if (folder == "")
            {
                var response = await dbx.Files.DownloadAsync(folder + "/" + filename);
                stream_data = await response.GetContentAsStringAsync();
            }
            else
            {
                var response = await dbx.Files.DownloadAsync("/" + folder + "/" + filename);
                stream_data= await response.GetContentAsStringAsync();
                Console.WriteLine(stream_data);
                            
            }
            Console.WriteLine(stream_data);
            String write_data = await Decrypt(Encoding.UTF8.GetBytes(stream_data));

            using (StreamWriter f = new StreamWriter(@"D:\test.txt", true))
            {
                f.Write(write_data);
            }
        }
        async Task Upload(DropboxClient dbx,String sourcefile_path,string dest_folder,string file)
        {
            String data;
            using (StreamReader sr = new StreamReader(sourcefile_path))
            {
                data = sr.ReadToEnd();
            }
            byte[] enc_data = await Secure_Upload1.Program.encrypt(data);
            String final_data = Encoding.ASCII.GetString(enc_data);

            Console.WriteLine(final_data);
            Console.Read();


            using (MemoryStream inp = new MemoryStream(Encoding.UTF8.GetBytes(final_data)))
            {
                Console.WriteLine("Entered");
                var upld = await dbx.Files.UploadAsync("/" + dest_folder + "/" + file, WriteMode.Overwrite.Instance, body:inp) ;
                Console.WriteLine(upld.Rev);
                Console.ReadLine();
               
            }
            
            }
        }
    }

