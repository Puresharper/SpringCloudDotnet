using System;
using System.Composition;
using System.Composition.Hosting;
using System.Runtime;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Web;

namespace ConsoleApplication1
{
    [DataContract]
    public class Product
    {
        [DataMember]
        public string Id { get; set; }

        [DataMember]
        public string Name { get; set; }
    }

    public interface IRepository
    {
        Product GetPoduct(string id);
    }

    [Export(typeof(IRepository))] //@Repository
    public class Repository : IRepository
    {
        public Product GetPoduct(string id)
        {
            return new Product() { Id = id, Name = $"Product_{ id }" };
        }
    }

    [ServiceContract]
    public interface IServiceProduct
    {
        [WebInvoke(Method = "GET", UriTemplate = "Product/{id}", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)] //GetMapping
        [OperationContract]
        Product GetProduct(string id);
    }

    [Export(typeof(IServiceProduct))] //@Service
    public class ServiceProduct : IServiceProduct
    {
        private IRepository m_Repository;

        [ImportingConstructor]
        public ServiceProduct(IRepository repository)
        {
            this.m_Repository = repository;
        }

        public Product GetProduct(string id)
        {
            return this.m_Repository.GetPoduct(id);
        }
    }

    public class SpringCloudApplication : IDisposable
    {
        private CompositionHost m_Container;
        private ServiceHost m_Host;

        private SpringCloudApplication(Type type)
        {
            this.m_Container = new ContainerConfiguration().WithAssembly(type.Assembly).CreateContainer();
            this.m_Host = new ServiceHost(this.m_Container.GetExport(type), new Uri("http://localhost:8080/"));
            this.m_Host.Description.Behaviors.Find<ServiceBehaviorAttribute>().InstanceContextMode = InstanceContextMode.Single;
            this.m_Host.AddServiceEndpoint(typeof(IServiceProduct), new WebHttpBinding(), type.Name).EndpointBehaviors.Add(new WebHttpBehavior());
            this.m_Host.Open();
        }

        static public SpringCloudApplication Run<T>()
            where T : class
        {
            return new SpringCloudApplication(typeof(T));
        }

        public void Dispose()
        {
            this.m_Host.Close();
            (this.m_Host as IDisposable).Dispose();
        }
    }

    static public class Program
    {
        static void Main(string[] args)
        {
            using (SpringCloudApplication.Run<IServiceProduct>())
            {
                Console.WriteLine("Press <Enter> to stop the service : http://localhost:8080/IServiceProduct/Product/{{id}}");
                Console.ReadLine();
            }
        }
    }
}
