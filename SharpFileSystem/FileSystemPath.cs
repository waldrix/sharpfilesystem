using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;

namespace SharpFileSystem
{
	public struct FileSystemPath : IEquatable<FileSystemPath>, IComparable<FileSystemPath>
	{
		public static readonly char DirectorySeparator = '/';
		public static FileSystemPath Root { get; }

		readonly string _path;

		// ReSharper disable once MemberCanBePrivate.Global
		public string Path => _path ?? "/";
		public bool IsDirectory => Path[Path.Length - 1] == DirectorySeparator;
		public bool IsFile => !IsDirectory;
		public bool IsRoot => Path.Length == 1;

		public string EntityName
		{
			get
			{
				var name = Path;
				if (IsRoot)
					return null;
				var endOfName = name.Length;
				if (IsDirectory)
					endOfName--;
				var startOfName = name.LastIndexOf(DirectorySeparator, endOfName - 1, endOfName) + 1;
				return name.Substring(startOfName, endOfName - startOfName);
			}
		}

		public FileSystemPath ParentPath
		{
			get
			{
				var parentPath = Path;
				if (IsRoot)
					throw new InvalidOperationException("There is no parent of root.");
				var lookaheadCount = parentPath.Length;
				if (IsDirectory)
					lookaheadCount--;
				var index = parentPath.LastIndexOf(DirectorySeparator, lookaheadCount - 1, lookaheadCount);
				Debug.Assert(index >= 0);
				parentPath = parentPath.Remove(index + 1);
				return new FileSystemPath(parentPath);
			}
		}

		static FileSystemPath() { Root = new FileSystemPath(DirectorySeparator); }

		FileSystemPath(char path) { _path = path.ToString(); }

		FileSystemPath(string path) { _path = path; }

		public static bool IsRooted(string s)
		{
			if (s.Length == 0)
				return false;
			return s[0] == DirectorySeparator;
		}

		public static FileSystemPath Parse(string s)
		{
			if (s == null)
				throw new ArgumentNullException("s");
			if (!IsRooted(s))
				throw new ParseException(s, "Path is not rooted");
			if (s.Contains(string.Concat(DirectorySeparator, DirectorySeparator)))
				throw new ParseException(s, "Path contains double directory-separators.");
			return new FileSystemPath(s);
		}

        [Pure]
		public FileSystemPath AppendPath(string relativePath)
		{
			if (IsRooted(relativePath))
				throw new ArgumentException("The specified path should be relative.", "relativePath");
			if (!IsDirectory)
				throw new InvalidOperationException("This FileSystemPath is not a directory.");
			return new FileSystemPath(Path + relativePath);
		}

		[Pure]
		public FileSystemPath AppendPath(FileSystemPath path)
		{
			if (!IsDirectory)
				throw new InvalidOperationException("This FileSystemPath is not a directory.");
			return new FileSystemPath(Path + path.Path.Substring(1));
		}

		[Pure]
		public FileSystemPath AppendDirectory(string directoryName)
		{
			if (directoryName.Contains(DirectorySeparator))
				throw new ArgumentException("The specified name includes directory-separator(s).", "directoryName");
			if (!IsDirectory)
				throw new InvalidOperationException("The specified FileSystemPath is not a directory.");
			return new FileSystemPath(Path + directoryName + DirectorySeparator);
		}

		[Pure]
		public FileSystemPath AppendFile(string fileName)
		{
			if (fileName.Contains(DirectorySeparator))
				throw new ArgumentException("The specified name includes directory-separator(s).", "fileName");
			if (!IsDirectory)
				throw new InvalidOperationException("The specified FileSystemPath is not a directory.");
			return new FileSystemPath(Path + fileName);
		}

		[Pure]
		public bool IsParentOf(FileSystemPath path) { return IsDirectory && Path.Length != path.Path.Length && path.Path.StartsWith(Path); }

		[Pure]
		public bool IsChildOf(FileSystemPath path) { return path.IsParentOf(this); }

		[Pure]
		public FileSystemPath RemoveParent(FileSystemPath parent)
		{
			if (!parent.IsDirectory)
				throw new ArgumentException("The specified path can not be the parent of this path: it is not a directory.");
			if (!Path.StartsWith(parent.Path))
				throw new ArgumentException("The specified path is not a parent of this path.");
			return new FileSystemPath(Path.Remove(0, parent.Path.Length - 1));
		}

		[Pure]
		public FileSystemPath RemoveChild(FileSystemPath child)
		{
			if (!Path.EndsWith(child.Path))
				throw new ArgumentException("The specified path is not a child of this path.");
			return new FileSystemPath(Path.Substring(0, Path.Length - child.Path.Length + 1));
		}

		[Pure]
		public string GetExtension()
		{
			if (!IsFile)
				throw new ArgumentException("The specified FileSystemPath is not a file.");
			var name = EntityName;
			var extensionIndex = name.LastIndexOf('.');
			if (extensionIndex < 0)
				return "";
			return name.Substring(extensionIndex);
		}

		[Pure]
		public FileSystemPath ChangeExtension(string extension)
		{
			if (!IsFile)
				throw new ArgumentException("The specified FileSystemPath is not a file.");
			var name = EntityName;
			var extensionIndex = name.LastIndexOf('.');
			if (extensionIndex >= 0)
				return ParentPath.AppendFile(name.Substring(0, extensionIndex) + extension);
			return Parse(Path + extension);
		}

		[Pure]
		public string[] GetDirectorySegments()
		{
			var path = this;
			if (IsFile)
				path = path.ParentPath;
			var segments = new LinkedList<string>();
			while (!path.IsRoot)
			{
				segments.AddFirst(path.EntityName);
				path = path.ParentPath;
			}

			return segments.ToArray();
		}

		[Pure]
		public int CompareTo(FileSystemPath other) { return string.Compare(Path, other.Path, StringComparison.OrdinalIgnoreCase); }

		[Pure]
		public override string ToString() { return Path; }

		[Pure]
		public override bool Equals(object obj)
		{
			if (obj is FileSystemPath path)
				return Equals(path);
			return false;
		}

		[Pure]
		public bool Equals(FileSystemPath other) { return other.Path.Equals(Path); }

		[Pure]
		public override int GetHashCode() { return Path.GetHashCode(); }

		public static bool operator ==(FileSystemPath pathA, FileSystemPath pathB) { return pathA.Equals(pathB); }

		public static bool operator !=(FileSystemPath pathA, FileSystemPath pathB) { return !(pathA == pathB); }
	}
}
