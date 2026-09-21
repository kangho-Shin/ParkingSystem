using APSMain.DbModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APSMain.Api.Response
{
    public class EnvConfigResponse
    {
        public string? Result { get; set; }
        public string? Message { get; set; }

        public List<Tdiscounttable>? Tdiscount { get; set; }
        public List<Tholiday>? Tholiday { get; set; }
        public List<Tparkfee>? Tparkfee { get; set; }
        public List<Tparkvariable>? Tparkvariable { get; set; }
    }

    public class EnvItemResponse
    {
        public string Result { get; set; } = "FAIL";
        public string Message { get; set; } = string.Empty;

        public List<Tdiscounttable> Tdiscount { get; set; } = new();
        public List<Tholiday> Tholiday { get; set; } = new();
        public List<Tparkfee> Tparkfee { get; set; } = new();
        public List<Tparkvariable> Tparkvariable { get; set; } = new();
    }
}
