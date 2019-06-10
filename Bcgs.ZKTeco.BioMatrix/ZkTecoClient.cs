

using Bcgs.ZKTeco.BioMatrix.Client;
using Bcgs.ZKTeco.BioMatrix.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Linq;

using System.Runtime.InteropServices;

namespace Bcgs.ZKTeco.BioMatrix
{
    public class ZkTecoClient : IDisposable
    {

        readonly DeviceManipulator manipulator = new DeviceManipulator();
        private ZkemService objZkeeper;
        private string HostName { get; set; }
        private int MachineNumber { get; set; }
        public string DeviceInfo { get; set; }
        public bool IsDeviceConnected { get; set; }
        private string DeviceIP { get; set; }
        private int PortNumber { get; set; } = 4370;

        public ZkTecoClient(string hostName, int portNumber = 4370, int machineNumber = 1)
        {
            this.HostName = hostName;
            this.PortNumber = portNumber;
            this.MachineNumber = machineNumber;

        }

        public bool ConnectToZKTeco()
        {

            try
            {


                if (IsDeviceConnected)
                {
                    //IsDeviceConnected = false;

                    return IsDeviceConnected;
                }

                if (this.DeviceIP == string.Empty || this.PortNumber < 1)
                    throw new Exception("The Device IP Address and Port is mandotory !!");


                bool isValidIpA = UniversalStatic.ValidateIP(this.DeviceIP);
                if (!isValidIpA)
                    throw new Exception("The Device IP is invalid !!");

                isValidIpA = UniversalStatic.PingTheDevice(this.DeviceIP);
                if (!isValidIpA)
                {
                    //retry
                    isValidIpA = UniversalStatic.PingTheDevice(this.DeviceIP);

                    if (!isValidIpA)
                        throw new Exception("The device at " + this.DeviceIP + ":" + this.PortNumber + " did not respond!!");
                }
                objZkeeper = new ZkemService(RaiseDeviceEvent);
                IsDeviceConnected = objZkeeper.Connect_Net(this.DeviceIP, this.PortNumber);

                if (IsDeviceConnected)
                {
                    this.DeviceInfo = manipulator.FetchDeviceInfo(objZkeeper, this.MachineNumber);

                }

            }
            catch (Exception ex)
            {
                throw ex;
            }

            return IsDeviceConnected;
        }

        private void RaiseDeviceEvent(object sender, string actionType)
        {
            switch (actionType)
            {
                case UniversalStatic.acx_Disconnect:
                    {
                        this.IsDeviceConnected = true;

                        break;
                    }

                default:
                    break;
            }

        }

        //public ICollection<AttendanceInfo> GetPullData_Click(object sender, EventArgs e)
        //{
        //        ICollection<AttendanceInfo> lstMachineInfo = manipulator.GetLogData(objZkeeper, this.MachineNumber);

        //        return lstMachineInfo;
        //}

        public ICollection<BioMatrixLog> GetBioMatrixData()
        {

            IPAddress[] ipaddress = Dns.GetHostAddresses(this.HostName);
            if (ipaddress.Length > 0)
            {
                this.DeviceIP = ipaddress[0].ToString();
            }

            this.ConnectToZKTeco();

            ICollection<BioMatrixLog> logs = manipulator.GetLogData(objZkeeper, this.MachineNumber);

            objZkeeper.Disconnect();

            return logs;

            //foreach (BioMatrixLog log in logs.OrderByDescending(x => x.DateOnlyRecord).Take(100))//TODO: Remove take
            //{

            //}



        }

        public void TestDBConnection()
        {

        }

        public void Dispose()
        {

            GC.SuppressFinalize(this);
        }
    }
}
