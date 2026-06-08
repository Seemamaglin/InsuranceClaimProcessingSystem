using System.Security.Cryptography;                                                             
   using System.Text;                                                                              
   using InsuranceClaimSystem.Application.Interfaces.External;                                     
   using Microsoft.Extensions.Configuration;                                                       
                                                                                                   
   namespace InsuranceClaimSystem.Infrastructure.Services.Encryption;                              
                                                                                                   
   public class AesEncryptionService : IPiiEncryptionService                                       
   {                                                                                               
       private readonly byte[] _key;                                                               
                                                                                                   
       public AesEncryptionService(IConfiguration configuration)                                   
       {                                                                                           
           var keyString = configuration["EncryptionSettings:Key"];                                
           if (string.IsNullOrWhiteSpace(keyString))                                               
               throw new InvalidOperationException("EncryptionSettings:Key is not configured.");   
                                                                                                   
           using var sha256 = SHA256.Create();                                                     
           _key = sha256.ComputeHash(Encoding.UTF8.GetBytes(keyString));                           
       }                                                                                           
                                                                                                   
       public string Encrypt(string plainText)                                                     
       {                                                                                           
           using var aes = Aes.Create();                                                           
           aes.Key = _key;                                                                         
           aes.GenerateIV();                                                                       
           aes.Mode = CipherMode.CBC;                                                              
           aes.Padding = PaddingMode.PKCS7;                                                        
                                                                                                   
           var plainBytes = Encoding.UTF8.GetBytes(plainText);                                     
           using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);                             
           var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);      
                                                                                                   
           var result = new byte[aes.IV.Length + cipherBytes.Length];                              
           Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);                                  
           Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);            
                                                                                                   
           return Convert.ToBase64String(result);                                                  
       }                                                                                           
                                                                                                   
       public string Decrypt(string cipherText)                                                    
       {                                                                                           
           var fullCipher = Convert.FromBase64String(cipherText);                                  
           const int ivSize = 16;                                                                  
                                                                                                   
           if (fullCipher.Length < ivSize)                                                         
               throw new ArgumentException("Cipher text is too short.");                           
                                                                                                   
           var iv = new byte[ivSize];                                                              
           Buffer.BlockCopy(fullCipher, 0, iv, 0, ivSize);                                         
                                                                                                   
           var cipherBytes = new byte[fullCipher.Length - ivSize];                                 
           Buffer.BlockCopy(fullCipher, ivSize, cipherBytes, 0, cipherBytes.Length);               
                                                                                                   
           using var aes = Aes.Create();                                                           
           aes.Key = _key;                                                                         
           aes.IV = iv;                                                                            
           aes.Mode = CipherMode.CBC;                                                              
           aes.Padding = PaddingMode.PKCS7;                                                        
                                                                                                   
           using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);                             
           var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);     
           return Encoding.UTF8.GetString(plainBytes);                                             
       }                                                                                           
                                                                                                   
       public string MaskAadhaar(string aadhaar)                                                   
       {                                                                                           
           if (string.IsNullOrWhiteSpace(aadhaar))                                                 
               return "XXXX-XXXX-0000";                                                            
                                                                                                   
           var digitsOnly = new string(aadhaar.Where(char.IsDigit).ToArray());                     
           if (digitsOnly.Length < 4)                                                              
               return "XXXX-XXXX-0000";                                                            
                                                                                                   
           var last4 = digitsOnly[^4..];                                                           
           return $"XXXX-XXXX-{last4}";                                                            
       }                                                                                           
                                                                                                   
       public Task<byte[]> EncryptAsync(byte[] plainData)                                          
           =>                                                                                      
 Task.FromResult(Encoding.UTF8.GetBytes(Encrypt(Encoding.UTF8.GetString(plainData))));             
                                                                                                   
       public Task<byte[]> DecryptAsync(byte[] cipherData)                                         
           =>                                                                                      
 Task.FromResult(Encoding.UTF8.GetBytes(Decrypt(Convert.ToBase64String(cipherData))));             
   }  