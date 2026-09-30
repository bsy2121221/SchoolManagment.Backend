using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace SchoolManagment.DataAccess.Context
{
    public interface IDbContext
    {
        IDbConnection CreateConnection();
    }
}
