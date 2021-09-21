using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Tooling.Connector;

namespace Parser462.Connector
{
    public class CrmConnector
    {


        private string connectionString =
            "Url=https://cer-dev-formbuilder.crm6.dynamics.com/;Username=adm_p_crm_admin@cer.gov.au;Password=R3dR@bbit123!;AuthType=OAuth;RequireNewInstance=true;AppId=51f81489-12ee-4a9e-aaae-a2591f45987d;RedirectUri=app://58145B91-0C36-4500-8554-080854F2AC97;LoginPrompt=Auto";
        //private string connectionString = $"Url=https://cer-test.crm6.dynamics.com;Username=adm_p_crm_admin@cer.gov.au;Password={Util.GetPassword("crm online - admp")};authtype=OAuth;RequireNewInstance=true;AppId=51f81489-12ee-4a9e-aaae-a2591f45987d;RedirectUri=app://58145B91-0C36-4500-8554-080854F2AC97;LoginPrompt=Auto";

        public IOrganizationService Service { get; private set; }

        public CrmServiceClient ServiceClient { get; private set; }

        public void PublishCustomisations()
        {
            PublishAllXmlRequest publishAllXml = new PublishAllXmlRequest();
            Service.Execute(publishAllXml);
        }

        public CrmConnector()
        {
            // Connect to the CRM web service using a connection string.
            ServiceClient = new CrmServiceClient(connectionString);
            // Cast the proxy client to the IOrganizationService interface.
            Service = ServiceClient.OrganizationWebProxyClient != null
                ? (IOrganizationService) ServiceClient.OrganizationWebProxyClient
                : (IOrganizationService) ServiceClient.OrganizationServiceProxy;
            //ServiceClient.OrganizationServiceProxy.Timeout = new TimeSpan(0, 30, 00);
        }
    }
}

