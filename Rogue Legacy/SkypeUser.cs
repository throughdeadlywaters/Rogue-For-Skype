namespace Rogue_Legacy
{
    public class SkypeUser
    {
        private string skypehandle;
        private string skypefullname;
        private string wanipaddress;
        private string lanipaddress;
        private string portnumber;

        public string SkypeFullName
        {
            get { return skypefullname; }
            set { skypefullname = value; }
        }

        public string SkypeHandle
        {
            get { return skypehandle; }
            set { skypehandle = value; }
        }

        public string RemoteIPAddress
        {
            get { return wanipaddress; }
            set { wanipaddress = value; }
        }

        public string LocalIPAddress
        {
            get { return lanipaddress; }
            set { lanipaddress = value; }
        }

        public string SkypePort
        {
            get { return portnumber; }
            set { portnumber = value; }
        }
    }
}
