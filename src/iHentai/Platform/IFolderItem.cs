﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace iHentai.Platform
{
    public interface IFolderItem : IStorageItem
    {
        Task<IEnumerable<IFolderItem>> GetFolders();
        Task<IEnumerable<IFileItem>> GetFiles();
    }

    public interface IFileItem : IStorageItem
    {
        string Extension { get; }
    }

    public interface IStorageItem
    {
        string Name { get; }
        string Path { get; }
        string Token { get; }
    }
}
