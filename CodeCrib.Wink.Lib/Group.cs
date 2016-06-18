using Newtonsoft.Json.Linq;

namespace CodeCrib.Wink.Lib
{
    public class Group//: Windows.UI.Xaml.DependencyObject
    {
        //public static readonly Windows.UI.Xaml.DependencyProperty CheckboxPoweredProperty = Windows.UI.Xaml.DependencyProperty.Register("CheckboxPowered", typeof(bool?), typeof(Group), new Windows.UI.Xaml.PropertyMetadata(null));

        public string Id { get; set; }
        public string Name { get; set; }
        public bool AllConnected { get; set; }
        public bool SomeConnected { get; set; }
        public bool? AllPowered { get; set; }
        public bool? SomePowered { get; set; }

        public bool? CheckBoxPowered { get; set; }

        public static Group FromJson(string json)
        {
            return Group.FromJson(JToken.Parse(json));
        }

        public static Group FromJson(JToken deviceToken)
        {
            Group group = new Group();

            group.Name = (string)deviceToken.SelectToken("name");
            group.Id = (string)deviceToken.SelectToken("group_id");
            //group.Model = (string)deviceToken.SelectToken("model_name");

            JToken readingAggregation = deviceToken.SelectToken("reading_aggregation");

            JToken connectedToken = readingAggregation.SelectToken("connection");
            if (connectedToken != null)
            {
                group.AllConnected = (bool)connectedToken.SelectToken("and");
                group.SomeConnected = (bool)connectedToken.SelectToken("or");
            }
            else
            {
                group.AllPowered = null;
                group.SomePowered = null;
            }

            JToken poweredToken = readingAggregation.SelectToken("powered");
            if (group.SomeConnected == true && poweredToken != null)
            {
                group.AllPowered = (bool)poweredToken.SelectToken("and");
                group.SomePowered = (bool)poweredToken.SelectToken("or");

                // If some are powered but not all
                if (group.SomePowered == true && group.AllPowered == false)
                {
                    // set checkbox to indeterminate
                    group.CheckBoxPowered = null;
                }
                else
                {
                    // otherwise it's all or nothing
                    group.CheckBoxPowered = group.AllPowered;
                }
            }
            else
            {
                group.AllPowered = null;
                group.SomePowered = null;
                group.CheckBoxPowered = false;
            }

            return group;
        }
    }
}