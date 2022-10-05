using Dallus;
using XDemo.Models;

namespace XDemo
{
    public partial class MainFm : Form
    {
        public MainFm()
        {
            InitializeComponent();
        }

        public void DoSomething()
        {
            // The following instance and static both should work the same
            
            // Using the instance version of Dallus
            var repo = new Repox("my connection string");
            var adrData = repo.GetPage<Address>(1, 25);
            var perData = repo.GetPageWhere<Person>(p => p.Id == 120, 1, 35);

            // Using the static version of Dallus
            var addrData = Repo.GetPage<Address>(1, 25);
            var personData = Repo.GetPageWhere<Person>(p => p.Id == 120, 1, 35);
        }
    }
}