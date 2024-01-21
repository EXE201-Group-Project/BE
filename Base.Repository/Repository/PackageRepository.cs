﻿using Base.Repository.Common;
using Base.Repository.Entity;
using Base.Repository.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.Repository.Repository;

internal class PackageRepository : BaseRepository<Package, int>, IPackageRepository
{
    public PackageRepository(ApplicationDbContext applicationDbContext) : base(applicationDbContext)
    {
    }
}
