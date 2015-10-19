using System;
using Castle.Facilities.TypedFactory;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Castle.Windsor.Installer;

namespace TypedFactoryWindsor
{
    #region interfaces and base implementations
    public interface IBase
    {
        string MyMessage();
    }

    #region marker interfaces
    public interface IExtOne : IBase
    {
    }

    public interface IExtTwo : IBase
    {
    }
    #endregion

    #region typed-factory interface - no implementation is required https://github.com/castleproject/Windsor/blob/master/docs/typed-factory-facility-interface-based.md
    public interface IExtFactory : IDisposable
    {
        IExtOne CreatExtOne();
        IExtTwo CreatExtTwo();
        void Destroy(IBase ext);
    }
    #endregion

    #region implementation of marker interfaces
    public class ExtOne : IExtOne
    {
        public string MyMessage()
        {
            return "one";
        }
    }

    public class ExtTwo : IExtTwo
    {
        public string MyMessage()
        {
            return "two";
        }
    }

    #endregion

    #endregion

    #region DummyWork class

    public class DummyWork
    {
        private readonly IExtFactory _factory;

        // the factory is injected into your class that uses the Resolve method
        // you do not use the container directly and do not inject it to other components
        public DummyWork(IExtFactory factory)
        {

            if (factory == null)
            {
                throw new ArgumentNullException("factory");
            }
            
            _factory = factory;
        }

        public void DoWork()
        {
            IBase ext = _factory.CreatExtOne();
            Console.WriteLine(ext.MyMessage());

            ext = _factory.CreatExtTwo();
            Console.WriteLine(ext.MyMessage());
        }

        // Auto install the class to the container
        public class DummyWorkInstaller : IWindsorInstaller
        {
            public void Install(IWindsorContainer container, IConfigurationStore store)
            {
                container.Register(Component.For<DummyWork>().ImplementedBy<DummyWork>());
            }
        }
    }
    #endregion

    class Program
    {
        #region Windsor container registrations
        private static IWindsorContainer Register()
        {
            var container = new WindsorContainer(); // create a new container
            container.AddFacility<TypedFactoryFacility>();
            container.Register(Component.For<IExtFactory>().AsFactory());

            // This will only register hierarchies of IBase from this assembly
            // you do not have to remember to register any new implementations
            // you DO have to add the Creator method for your interface in the Typed-Factory
            container.Register(Classes.FromThisAssembly()
                                        .Where(type => typeof(IBase).IsAssignableFrom(type)) 
                                        .WithServiceDefaultInterfaces()
                                        .LifestyleTransient());

            container.Install(FromAssembly.This()); // find all installers in executing assembly and install them

            return container;
        }
        #endregion

        static void Main(string[] args)
        {
            // Generally the controller in WebAPI will be the created from the container and all dependencies passed down
            // in a WinService the service will be spawned from the container and all dependencies passed down (TopShelf host)
            using (var container = Register())
            {
                var dummy = container.Resolve<DummyWork>(); // this should be the root of your application, resolve the Api/Controller or WinService
                dummy.DoWork();
            } // the container is disposed and all resolvd instances are released (the factory)
        }
    }
}
