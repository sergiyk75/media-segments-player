using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Practices.Prism.Commands;
using System.Collections.Concurrent;
using Microsoft.Practices.Prism.Mvvm;

namespace MediaPlaybackLib
{
    public class NotifyObject: INotifyPropertyChanged
    {
        // default prefixes for automatic command plumbing
        private const string ExecutePrefix = "Execute";
        private const string CanExecutePrefix = "CanExecute";
        
        private const BindingFlags InstanceBindingFlags =
           BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        // dictionaries which map out our relationships and dependencies 
        private readonly IDictionary<string, List<MemberInfo>> commandMap;
        private readonly IDictionary<string, List<MemberInfo>> methodMap;
        private readonly IDictionary<string, List<MemberInfo>> propertyMap;
        private readonly Dictionary<string, object> values = new Dictionary<string, object>();

        // Cache for our dictionaries, as creating these dictionaries can take significant time and we do this for the same type many times
        private static ConcurrentDictionary<Type, IDictionary<string, List<MemberInfo>>> cacheCommandMap = new ConcurrentDictionary<Type, IDictionary<string, List<MemberInfo>>>();
        private static ConcurrentDictionary<Type, IDictionary<string, List<MemberInfo>>> cahceMethodMap = new ConcurrentDictionary<Type, IDictionary<string, List<MemberInfo>>>();
        private static ConcurrentDictionary<Type, IDictionary<string, List<MemberInfo>>> cachePropertyMap = new ConcurrentDictionary<Type, IDictionary<string, List<MemberInfo>>>();

        /// <summary>
        /// Reflects on properties, methods, commands, and verified dependencies.
        /// </summary>
        public NotifyObject()
        {
            var currentType = GetType();
            if (cacheCommandMap.ContainsKey(currentType) && cachePropertyMap.ContainsKey(currentType) && cahceMethodMap.ContainsKey(currentType))
            {
                this.commandMap = cacheCommandMap[currentType];
                this.propertyMap = cachePropertyMap[currentType];
                this.methodMap = cahceMethodMap[currentType];
            }
            else
            {
                // map all the depends upon properties
                propertyMap = MapDependencies(currentType.GetProperties());

                MemberInfo[] methods = currentType.GetMethods(InstanceBindingFlags).Cast<MemberInfo>().ToArray();

                // map all our depends upon methods that are not command related
                methodMap =
                    MapDependencies(
                        methods.Where(method => !method.Name.StartsWith(CanExecutePrefix, StringComparison.Ordinal)));

                // map all the commands with depends upon attribute
                commandMap =
                    MapDependencies(
                        methods.Where(method => method.Name.StartsWith(CanExecutePrefix, StringComparison.Ordinal)));

                VerifyDependencies();

                cacheCommandMap.AddOrUpdate(currentType, this.commandMap, (type, oldValue) => this.commandMap);
                cachePropertyMap.AddOrUpdate(currentType, this.propertyMap, (type, oldValue) => this.propertyMap);
                cahceMethodMap.AddOrUpdate(currentType, this.methodMap, (type, oldValue) => this.methodMap);
            }

            CreateCommands();
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        /// <summary>
        /// Get all the command names minus the prefix
        /// </summary>
        private IEnumerable<string> CommandNames
        {
            get
            {
                return from method in GetType().GetMethods(InstanceBindingFlags)
                       where method.Name.StartsWith(ExecutePrefix, StringComparison.Ordinal)
                       select StripLeft(method.Name, ExecutePrefix.Length);
            }
        }

        /// <summary>
        /// Get the dynamic value of type T and name, with a default value
        /// </summary>
        /// <typeparam name="T">the type</typeparam>
        /// <param name="name">the name of the value</param>
        /// <param name="defaultValue">the default value if doesn't exist</param>
        /// <returns>the default value if has not been previous set using Set or the Set value</returns>
        private T Get<T>(string name, T defaultValue = default(T))
        {
            if (values.ContainsKey(name))
            {
                return (T)values[name];
            }

            return defaultValue;
        }

        /// <summary>
        /// Get the dynamic value of type T and name, with a default value
        /// </summary>
        /// <typeparam name="T">the type</typeparam>
        /// <param name="name">the name of the value</param>
        /// <param name="initialValue">the function to call which sets the initial value if not already set</param>
        /// <returns>the default value if has not been previous set using Set or the Set value</returns>
        private T Get<T>(string name, Func<T> initialValue) 
        {
            if (values.ContainsKey(name))
            {
                return (T)values[name];
            }

            Set(name, initialValue());
            return Get<T>(name);
        }

        /// <summary>
        /// Get the property value using an expression/>
        /// </summary>
        /// <typeparam name="T">the type</typeparam>
        /// <param name="expression">expression</param>
        /// <returns>the value</returns>
        protected T Get<T>(Expression<Func<T>> expression)
        {
            return Get<T>(PropertySupport.ExtractPropertyName(expression));
        }

        /// <summary>
        /// Get the property value using an expression/>
        /// </summary>
        /// <typeparam name="T">the type</typeparam>
        /// <param name="expression">expression</param>
        /// <param name="defaultValue">default value to return </param>
        /// <returns>the value, or default if expression doesn't find the value</returns>
        protected T Get<T>(Expression<Func<T>> expression, T defaultValue)
        {
            return Get(PropertySupport.ExtractPropertyName(expression), defaultValue);
        }

        /// <summary>
        /// Get the property value using an expression/>
        /// </summary>
        /// <typeparam name="T">the type</typeparam>
        /// <param name="expression">expression</param>
        /// <param name="initialValue">using a function to return an initial (default) value</param>
        /// <returns>the value found or the value returned from the initialValue function</returns>
        protected T Get<T>(Expression<Func<T>> expression, Func<T> initialValue) where T : class
        {
            return Get(PropertySupport.ExtractPropertyName(expression), initialValue);
        }

        private readonly object setSyncLock = new object();

        /// <summary>
        /// Set the property of given name to value of type T. RaisePropertyChanged will be called.
        /// </summary>
        /// <typeparam name="T">the type of the value</typeparam>
        /// <param name="name">the property name to use</param>
        /// <param name="value">the value of that property</param>
        private void Set<T>(string name, T value, Action changedAction = null) 
        {
            lock (setSyncLock)
            {
                object oldValue;
                if (values.TryGetValue(name, out oldValue))
                {
                    if (oldValue == null && value == null)
                        return;

                    if (oldValue != null && oldValue.Equals(value))
                        return;

                    values[name] = value;
                }
                else if (value != null)
                {
                    values.Add(name, value);
                }
                else
                    return;
            }

            RaisePropertyChanged(name);

            if (changedAction != null)
                changedAction.Invoke();
        }

        /// <summary>
        /// Set property name from expression to value
        /// </summary>
        /// <typeparam name="T">the type of the value</typeparam>
        /// <param name="expression">the expression to evaluate for property name</param>
        /// <param name="value">the value of the property</param>
        protected void Set<T>(Expression<Func<T>> expression, T value, Action changedAction) 
        {
            if (changedAction == null)
                throw new ArgumentNullException("changedAction");
            Set(PropertySupport.ExtractPropertyName(expression), value, changedAction);
        }

        /// <summary>
        /// Set property name from expression to value
        /// </summary>
        /// <typeparam name="T">the type of the value</typeparam>
        /// <param name="expression">the expression to evaluate for property name</param>
        /// <param name="value">the value of the property</param>
        protected void Set<T>(Expression<Func<T>> expression, T value)
        {
            Set(PropertySupport.ExtractPropertyName(expression), value, null);
        }

        /// <summary>
        /// Raise property changed for the property of the provided name. All depends upon will also be raised.
        /// </summary>
        /// <param name="propertyName">the name of the property</param>
        [SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate")]
        protected void RaisePropertyChanged(string propertyName)
        {
            if (!string.IsNullOrEmpty(propertyName))
                ExecuteDependentMethods(propertyName);

            OnPropertyChanged(propertyName);

            if (string.IsNullOrEmpty(propertyName))
                return;

            if (propertyMap.ContainsKey(propertyName))
                propertyMap[propertyName].ForEach(mi => RaisePropertyChanged(mi.Name));

            
            FireChangesOnDependentCommands(propertyName);
        }

        /// <summary>
        /// Raise property changed for all properties
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate")]
        protected void RaiseAllPropertiesChanged()
        {
            OnPropertyChanged(null);
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Raises this object's PropertyChanged event.
        /// </summary>
        /// <param name="propertyExpression">A Lambda expression representing the property that has a new value</param>
        [SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate",
            Justification = "Method used to raise an event")]
        protected void RaisePropertyChanged<T>(Expression<Func<T>> propertyExpression)
        {
            string propertyName = PropertySupport.ExtractPropertyName(propertyExpression);
            RaisePropertyChanged(propertyName);
        }

        /// <summary>
        /// Execute all depends upon mapped methods which depend upon property of name
        /// </summary>
        /// <param name="name">the name of the property watched</param>
        private void ExecuteDependentMethods(string name)
        {
            if (methodMap.ContainsKey(name))
                methodMap[name].ForEach(mi => ((MethodInfo)mi).Invoke(this, null));
        }

        /// <summary>
        /// Raise can execute changed dependent commands on property name
        /// </summary>
        /// <param name="name">name of the property</param>
        private void FireChangesOnDependentCommands(string name)
        {
            if (commandMap.ContainsKey(name))
                commandMap[name].ForEach(mi => RaiseCanExecuteChanged(mi.Name));
        }

        /// <summary>
        /// Raise Can Execute Changed Event. 
        /// </summary>
        /// <param name="canExecuteName">the name of the CanExecute method</param>
        [SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate")]
        private void RaiseCanExecuteChanged(string canExecuteName)
        {
            string commandName = StripLeft(canExecuteName, CanExecutePrefix.Length);
            var command = Get<DelegateCommand<object>>(commandName);
            if (command == null)
                return;

            command.RaiseCanExecuteChanged();
        }


        /// <summary>
        /// Create all of the default commands where the command prefixes are defined in the derived class
        /// </summary>
        private void CreateCommands()
        {
            foreach (var name in CommandNames)
                Set(name, new DelegateCommand<object>(x => ExecuteCommand(name, x), x => CanExecuteCommand(name, x)));
        }

        /// <summary>
        /// Execute the command with the given name and parameters
        /// </summary>
        /// <param name="name">the command name</param>
        /// <param name="parameter">the parameter</param>
        private void ExecuteCommand(string name, object parameter)
        {
            MethodInfo methodInfo = GetType().GetMethod(ExecutePrefix + name, InstanceBindingFlags);
            if (methodInfo == null) return;

            methodInfo.Invoke(this, methodInfo.GetParameters().Length == 1 ? new[] { parameter } : null);
        }

        /// <summary>
        /// Can the command execute
        /// </summary>
        /// <param name="name">the name of the command</param>
        /// <param name="parameter">command parameter</param>
        /// <returns>true if the command can execute, false otherwise</returns>
        private bool CanExecuteCommand(string name, object parameter)
        {
            MethodInfo methodInfo = GetType().GetMethod(CanExecutePrefix + name, InstanceBindingFlags);
            if (methodInfo == null) return true;

            return (bool)methodInfo.Invoke(this, methodInfo.GetParameters().Length == 1 ? new[] { parameter } : null);
        }

        /// <summary>
        /// Map all our dependencies (DependsUpon) existing in the getInfo function.
        /// </summary>
        /// <returns>the dictionary of mapped pairs</returns>
        private static IDictionary<string, List<MemberInfo>> MapDependencies(IEnumerable<MemberInfo> methods)
        {
            Dictionary<MemberInfo, List<string>> dependencyMap = methods.ToDictionary(
                m => m,
                m => m.GetCustomAttributes(typeof(DependsUponAttribute), true)
                         .Cast<DependsUponAttribute>()
                         .Select(a => a.DependencyName)
                         .ToList());

            return Invert(dependencyMap);
        }

        /// <summary>
        /// Invert the dictionary
        /// </summary>
        /// <param name="map">the dictionary to invert</param>
        /// <returns></returns>
        private static IDictionary<string, List<MemberInfo>> Invert(IDictionary<MemberInfo, List<string>> map)
        {
            // flatten the map dictionary and select the distinct values
            var flattened = from key in map.Keys
                            from value in map[key]
                            select new { Key = key, Value = value };

            IEnumerable<string> uniqueValues = flattened.Select(x => x.Value).Distinct();

            // re-create the dictionary with unique keys and flattened values.
            return uniqueValues.ToDictionary(
                x => x,
                x => (from item in flattened
                      where item.Value == x
                      select item.Key).ToList());
        }

        /// <summary>
        /// Verify dependencies of properties and methods to <see cref="DependsUponAttribute"/>. Basically determine that all properties
        /// defined in all the DependsUponAttributes actually exist. 
        /// </summary>
        /// <exception cref="ArgumentException">Thrown if the property doesn't exist</exception>
        private void VerifyDependencies()
        {
            IEnumerable<MemberInfo> methods = GetType().GetMethods(InstanceBindingFlags);
            PropertyInfo[] properties = GetType().GetProperties();

            IEnumerable<string> propertyNames = methods.Union(properties)
                .SelectMany(
                    method =>
                    method.GetCustomAttributes(typeof(DependsUponAttribute), true).Cast<DependsUponAttribute>())
                .Where(attribute => attribute.VerifyStaticExistence)
                .Select(attribute => attribute.DependencyName);

            foreach (var name in propertyNames)
                VerifyDependency(name);
        }

        /// <summary>
        /// Verify that the provided property name exists in our object
        /// </summary>
        /// <param name="propertyName">the property name to verify</param>
        /// <exception cref="ArgumentException">Thrown if the property doesn't exist</exception>
        private void VerifyDependency(string propertyName)
        {
            PropertyInfo property = GetType().GetProperty(propertyName);
            if (property == null)
                throw new InvalidOperationException("DependsUpon Property Does Not Exist: " + propertyName);
        }

        private static string StripLeft(string value, int length)
        {
            return value.Substring(length, value.Length - length);
        }
    }
}
