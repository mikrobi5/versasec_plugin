namespace VSec.DotNet.CmsCore.Wrapper.Models
{
    public class CertificateImport
    {
        public uint UserRole { get; set; }
        public string RolePin { get; set; }
        public string CertificateFilename { get; set; }
        public string CertificatePin { get; set; }
        public uint ContainerId { get; set; }
        public int KeySpec { get; set; }
        public string ErrorResult { get; set; }
    }
}