namespace Installer;

public interface IPage
{
    /// <summary>
    /// 该页面是否需要显示
    /// </summary>
    public bool ShouldShow => true;
    /// <summary>
    /// 是否允许用户进行下一步
    /// </summary>
    public bool IsNextStepAvailable => true;
    /// <summary>
    /// 是否允许用户返回上一步
    /// </summary>
    public bool IsLastStepAvailable => true;
}
