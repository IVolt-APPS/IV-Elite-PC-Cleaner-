using System;
using System.Collections.Generic;
using System.Linq;

using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace IVolt.Apps.IV_Elite_PC_Cleaner
{
	public static class File_Helper
	{
		public static string ComputeHash(string filePath)
		{
			using (FileStream stream = File.OpenRead(filePath))
			using (SHA256 sha = SHA256.Create())
			{
				byte[] hash = sha.ComputeHash(stream);
				StringBuilder sb = new StringBuilder();
				foreach (byte b in hash)
					sb.Append(b.ToString("x2"));
				return sb.ToString();
			}
		}
	}
}
