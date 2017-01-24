using System;

namespace MediaPlaybackLib
{
    /// <summary>
    /// Attribute that enables you to mark a property/method DependsUpon("X"). When the class derives from ViewModelBase
    /// this will automatically raise propertychanged event on the marked property when the indicated property/method has completed.
    /// </summary>
    /// <remarks>
    /// This is most beneficial when you have a setter that is responsible for calling both the INPC of itself, but also of another 
    /// property. Not only does that seem backwards, but it also violates the single responsibility principle. This allows us to
    /// invert that dependency.
    /// 
    /// [DependsUpon("Score")]
    /// public int Percentage
    /// {
    ///    get { return (int)(100 * Score); }
    /// }
    ///
    /// [DependsUpon("Percentage")]
    /// public string Output
    /// {
    ///    get { return "You scored " + Percentage + "%."; }
    /// }
    /// 
    /// Automatic Method Execution
    /// This one is extremely similar to the previous, but it deals with method execution as opposed to property.  When you want to execute 
    /// a method triggered by property changes, let the method declare the dependency instead of the other way around.
    /// 
    ///  public double Score
    ///    {
    ///        get { return Get(() => Score); }
    ///        set { Set(() => Score, value); }
    ///    }
    /// 
    /// [DependsUpon("Score")]
    /// public void WhenScoreChanges()
    /// {
    ///        // Handle this case
    /// }
    /// 
    /// This also working on CanExecute methods as well. (See ViewModelBase)
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAttributes", Justification = "Required to be a constraint using Where and cannot be sealed as a result.")]
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = true)]
    public class DependsUponAttribute : Attribute
    {
        /// <summary>
        /// Name of the dependency.
        /// </summary>
        public string DependencyName { get; private set; }

        /// <summary>
        /// If set to true, the existence of this Property should be verified.
        /// </summary>
        public bool VerifyStaticExistence { get; set; }

        public DependsUponAttribute(string dependencyName)
        {
            DependencyName = dependencyName;
        }
    }
}