using System;
using System.IO;
using System.Linq;

namespace StoryFormatter
{

	public static class Extensions
	{

		public static string NameWithoutExtension(this FileSystemInfo fio)
		{
			return Path.GetFileNameWithoutExtension(fio.Name);
		}

		public static FileInfo Combine(this DirectoryInfo dio, string part)
		{
			return new FileInfo(Path.Combine(dio.FullName, part));
		}

	}

}
