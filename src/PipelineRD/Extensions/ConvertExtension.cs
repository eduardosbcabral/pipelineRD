namespace PipelineRD.Extensions
{
    public static class ConvertExtension
    {
        public static void ConvertTo<TConvert>(this TConvert entity, TConvert convert)
        {
 
            foreach(var property in entity.GetType().GetProperties())
            {
                foreach(var destProperty in convert.GetType().GetProperties())
                {
                    if (destProperty.Name == property.Name)
                    {
                        destProperty.SetValue(convert, property.GetValue(entity), System.Reflection.BindingFlags.NonPublic |
                            System.Reflection.BindingFlags.Public |
                            System.Reflection.BindingFlags.DeclaredOnly |
                            System.Reflection.BindingFlags.GetProperty, null, null, null);

                        break;
                    }
                }
            }


        }
    }
}
