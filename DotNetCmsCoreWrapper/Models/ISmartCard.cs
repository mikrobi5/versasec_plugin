namespace VSec.DotNet.CmsCore.Wrapper.Models
{
    public interface ISmartCard
    {
        bool ChangeUserPin();

        bool UnblockUserPin();

        bool LoginUser();
    }
}