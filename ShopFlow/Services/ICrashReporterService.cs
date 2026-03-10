using System;
using System.Threading.Tasks;

namespace BrenoMachado.ShopFlow.Services
{
    public interface ICrashReporterService
    {
        Task ReportExceptionAsync(Exception exception, string context = "");
    }
}
