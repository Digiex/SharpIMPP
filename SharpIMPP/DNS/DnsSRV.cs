using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpIMPP.DNS
{
    public class DnsSRV
    {
        [DllImport("dnsapi", EntryPoint = "DnsQuery_W", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
        private static extern int DnsQuery([MarshalAs(UnmanagedType.VBByRefStr)]ref string pszName, QueryTypes wType, QueryOptions options, int aipServers, ref IntPtr ppQueryResults, int pReserved);

        [DllImport("dnsapi", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern void DnsRecordListFree(IntPtr pRecordList, int FreeType);

        public static SRVRecord[] GetSRVRecords(string domain)
        {

            IntPtr ptr1 = IntPtr.Zero;
            IntPtr ptr2 = IntPtr.Zero;
            SRV_Record recSrv;
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                throw new NotSupportedException();
            }
            ArrayList list1 = new ArrayList();
            int num1 = DnsSRV.DnsQuery(ref domain, QueryTypes.DNS_TYPE_SRV, QueryOptions.DNS_QUERY_BYPASS_CACHE, 0, ref ptr1, 0);
            if (num1 != 0)
            {
                throw new Win32Exception(num1);
            }
            for (ptr2 = ptr1; !ptr2.Equals(IntPtr.Zero); ptr2 = recSrv.pNext)
            {
                recSrv = (SRV_Record)Marshal.PtrToStructure(ptr2, typeof(SRV_Record));
                if (recSrv.wType == 0x0021)
                {
                    //string text1 = Marshal.PtrToStringAuto(recSrv.pNameTarget);
                    list1.Add(new SRVRecord(Marshal.PtrToStringAuto(recSrv.pNameTarget), recSrv.wPriority, recSrv.wWeight, recSrv.wPort));
                }
            }
            DnsSRV.DnsRecordListFree(ptr1, 0);
            return (SRVRecord[])list1.ToArray(typeof(SRVRecord));
        }

        private enum QueryOptions
        {
            DNS_QUERY_ACCEPT_TRUNCATED_RESPONSE = 1,
            DNS_QUERY_BYPASS_CACHE = 8,
            DNS_QUERY_DONT_RESET_TTL_VALUES = 0x100000,
            DNS_QUERY_NO_HOSTS_FILE = 0x40,
            DNS_QUERY_NO_LOCAL_NAME = 0x20,
            DNS_QUERY_NO_NETBT = 0x80,
            DNS_QUERY_NO_RECURSION = 4,
            DNS_QUERY_NO_WIRE_QUERY = 0x10,
            DNS_QUERY_RESERVED = -16777216,
            DNS_QUERY_RETURN_MESSAGE = 0x200,
            DNS_QUERY_STANDARD = 0,
            DNS_QUERY_TREAT_AS_FQDN = 0x1000,
            DNS_QUERY_USE_TCP_ONLY = 2,
            DNS_QUERY_WIRE_ONLY = 0x100
        }

        private enum QueryTypes
        {
            //DNS_TYPE_A = 1,
            //DNS_TYPE_NS = 2,
            //DNS_TYPE_CNAME = 5,
            //DNS_TYPE_SOA = 6,
            //DNS_TYPE_PTR = 12,
            //DNS_TYPE_HINFO = 13,
            //DNS_TYPE_MX = 15,
            //DNS_TYPE_TXT = 16,
            //DNS_TYPE_AAAA = 28,
            DNS_TYPE_SRV = 0x0021
        }

        //[StructLayout(LayoutKind.Sequential)]
        //private struct MXRecord
        //{
        //    public IntPtr pNext;
        //    public string pName;
        //    public short wType;
        //    public short wDataLength;
        //    public int flags;
        //    public int dwTtl;
        //    public int dwReserved;
        //    public IntPtr pNameExchange;
        //    public short wPreference;
        //    public short Pad;
        //}

        [StructLayout(LayoutKind.Sequential)]
        private struct SRV_Record
        {
            public IntPtr pNext;
            public string pName;
            public short wType;
            public short wDataLength;
            public int flags;
            public int dwTtl;
            public int dwReserved;

            public IntPtr pNameTarget;
            public short wPriority;
            public short wWeight;
            public short wPort;
            public short Pad;
        }

        public class SRVRecord
        {
            public string NameTarget { get; private set; }
            public short Priority { get; private set; }
            public short Weight { get; private set; }
            public short Port { get; private set; }

            public SRVRecord(string NameTarget, short Priority, short Weight, short Port)
            {
                this.NameTarget = NameTarget;
                this.Priority = Priority;
                this.Weight = Weight;
                this.Port = Port;
            }
            public override string ToString()
            {
                return NameTarget+":"+Port;
            }
        }

    }
}