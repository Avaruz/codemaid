using System.Threading.Tasks;

namespace ASGV.CodeMaid.Integration
{
    internal interface ISwitchableFeature
    {
        Task SwitchAsync(bool on);
    }
}