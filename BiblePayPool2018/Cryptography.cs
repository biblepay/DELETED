using System;
using System.Security.Cryptography;
using System.Text;


namespace BiblePayPool2018
{

    public static class modCryptography
    {  
	    private static TripleDESCryptoServiceProvider TripleDes = new TripleDESCryptoServiceProvider();
	    public static string MerkleRoot = "0xda43abf15a2fcd57ceae9ea0b4e0d872981e2c0b72244466650ce6010a14efb8";
    
	    private static byte[] TruncateHash(string key, int length)
	    {
		    SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider();
		    byte[] keyBytes = System.Text.Encoding.Unicode.GetBytes(key);
		    byte[] hash = sha1.ComputeHash(keyBytes);
		    Array.Resize(ref hash, length);
		    return hash;
	    }

	    public static string ToBase64(string data)
	    {
		    try {
			    return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(data));
		    } catch (Exception ex) {
			    return "";
		    }
	    }
	    public static string FromBase64(string data)
	    {
		    try
            {
			    return Encoding.UTF8.GetString(Convert.FromBase64String(data));
		    }
            catch (Exception ex)
            {
			    return "";
		    }
	    }
	    public static string ByteArrayToHexString(byte[] ba)
	    {
		    StringBuilder hex = default(StringBuilder);
		    hex = new StringBuilder(ba.Length * 2);
		    foreach (byte b in ba)
            {
			    hex.AppendFormat("{0:x2}", b);
		    }
		    return hex.ToString();
	    }
	
	    private static void Merkle(string sSalt)
	    {
		    try
            {
			    TripleDes.Key = TruncateHash(MerkleRoot + sSalt.Substring(sSalt.Length-4,4), TripleDes.KeySize / 8);
			    TripleDes.IV = TruncateHash("", TripleDes.BlockSize / 8);
		    }
            catch (Exception)
            {
                return;
		    }
	    }

	    public static string Des3EncryptData(string plaintext)
	    {
			    Merkle(MerkleRoot);
			    byte[] plaintextBytes = System.Text.Encoding.Unicode.GetBytes(plaintext);
			    System.IO.MemoryStream ms = new System.IO.MemoryStream();
			    CryptoStream encStream = new CryptoStream(ms, TripleDes.CreateEncryptor(), System.Security.Cryptography.CryptoStreamMode.Write);
                encStream.Write(plaintextBytes, 0, plaintextBytes.Length);
			    encStream.FlushFinalBlock();
                try 
                {
				    return Convert.ToBase64String(ms.ToArray());
			    } 
                catch (Exception) 
                {
			    }
                return String.Empty;
	    }

	    public static string Des3DecryptData(string encryptedtext)
	    {

			   Merkle(MerkleRoot);
	           byte[] encryptedBytes = Convert.FromBase64String(encryptedtext);
    		   System.IO.MemoryStream ms = new System.IO.MemoryStream();
			   CryptoStream decStream = new CryptoStream(ms, TripleDes.CreateDecryptor(), System.Security.Cryptography.CryptoStreamMode.Write);
        	   decStream.Write(encryptedBytes, 0, encryptedBytes.Length);
			   decStream.FlushFinalBlock();
			   return System.Text.Encoding.Unicode.GetString(ms.ToArray());
        }

        public static void Log(string sData, System.Web.HttpServerUtility s)
	    {
		    try 
            {
			    string sPath = null;
                sPath = s.MapPath("usgd.log");
			    System.IO.StreamWriter sw = new System.IO.StreamWriter(sPath, true);
			    sw.WriteLine(System.DateTime.Now.ToString() + ", " + sData);
			    sw.Close();
		    }
            catch (Exception)
            {
		    }
	    }

        public static string GetBibleFolder()
        {
                string sTemp = null;
                sTemp = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\BiblepayCore\\";
                return sTemp;
        }

    }
}