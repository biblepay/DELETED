using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BiblePayPool2018
{
    public class SQLDetective
    {

        public string FromFields { get; set; }
        public string WhereClause { get; set; }
        public string Table { get; set; }
        public string InnerJoin { get; set; }
        public string OrderBy { get; set; }

        public string GenerateQuery()
        {
            string sql = "SELECT " + FromFields + " FROM " + Table + " " + InnerJoin;
            if (WhereClause.Length > 0)
            {
                sql += " WHERE " + WhereClause;
            }
            if (OrderBy.Length > 0)
            {
                sql += " ORDER BY " + OrderBy;
            }

            return sql;

        }
    }
}