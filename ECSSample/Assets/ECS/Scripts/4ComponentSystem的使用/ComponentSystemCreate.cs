using Unity.Entities;
using Unity.Collections;

//好久不见了，之前忙于学业，现在终于考完了
//接下来更新频率会高一些
//之前有人问我上一期视频中教的给class用IComponentData为什么无效，后来我发现应该和Entities装的版本有关，我现在的教程将一直基于最新版本
//如果是较老的版本可能会无法使用一些API，如果新版本去掉了什么以前我讲过的API的话我也会在最新的视频里提及
//今天我将讲ECS中的S，如何使用一个System，系统分为三大类，而现在讲的是最基础的一类，ComponentSystem

//本工程文件已开源于https://github.com/ThreeIce/ThreeIceECSSample




//该系统将操作上期视频创建的Entity
//System不像MonoBehaviour那样，必须要手动拖到一个GameObject上才能运行，启动时unity会通过反射自动把所有的系统添加到默认生成的世界中
//也可以通过一些代码控制这个过程，但这部分代码不再今天讲的范围内
//[AlwaysUpdateSystem]这个特性可以让系统即使不满足运行条件也一直运行下去，不被Unity自动暂停
//[UpdateAfter(xx)][UpdateBefore(xx)]可以控制系统的执行顺序
//[DisableAutoCreation]可以让系统不在启动时被添加到默认世界中
public class TestComponentSystem : ComponentSystem
{
    /// <summary>
    /// 用来获取拥有指定组件实体和修改组件值的工具，不过不推荐通过这玩意修改组件值，不直观，为什么接下来你们就知道了
    /// 同一个系统可以有多个eq，没有限制
    /// </summary>
    private EntityQuery eq;
    /// <summary>
    /// 该函数会在系统被创建时调用，相当于MonoBehaviour中的Start
    /// </summary>
    protected override void OnCreate()
    {
        CreateEntityQuery1();
        SetFilter();
    }
    /// <summary>
    /// 普通eq的创建
    /// </summary>
    private void CreateEntityQuery1(){
        //最基本的创建eq的方法，该函数是系统所具有的，实质是调用EntityManager上的同名函数
        //意为搜索同时拥有TestComponentData组件和TestSharedComponentData的实体
        //ReadWrite在这里就有用了，它涉及了系统的调用排序和一系列复杂的依赖关系，总之如果你要修改获取的组件的的值就用ReadWrite，不需要就ReadOnly
        eq = this.GetEntityQuery(ComponentType.ReadWrite<TestComponentData>(),ComponentType.ReadOnly<TestSharedComponentData>());
        //标记该EntityQuery是系统执行所依赖的，如果该eq搜索不到拥有目标组件的实体系统就不会执行，省性能
        this.RequireForUpdate(eq);
    }
    /// <summary>
    /// 复杂eq的创建
    /// </summary>
    private void CreateEntityQuery2(){
        //EntityQuery依赖信息的创建
        EntityQueryDesc desc = new EntityQueryDesc(){
            //All内所有的组件实体都必须包含
            All = new ComponentType[]{ComponentType.ReadWrite<TestComponentData>()},
            //Any内所有的组件实体必须包含至少一个
            Any = new ComponentType[]{ComponentType.ReadOnly<TestSharedComponentData>()},
            //None内所有的组件实体都不能包含（此时ReadOnly和RadWrite区别不大）
            None = new ComponentType[]{ComponentType.ReadOnly<TestBufferElementData>()}
        };
        eq = GetEntityQuery(desc);
    }
    /// <summary>
    /// eq的高级操作，过滤
    /// </summary>
    private void SetFilter(){
        //筛选具有指定组件值的实体的方法
        //意思是只要TestSharedComponentData的Value为2的组件，如果是1或3都不要，起筛选作用，不过目前似乎只有SharedComponent支持作为筛选依据
        eq.SetSharedComponentFilter(new TestSharedComponentData(){ Value = 2});//value为1的筛不出来，为什么可以思考一下（滑稽）
        //筛选组件版本改过的实体，这个版本系统很坑，不仅修改数据会更改，作为ReadWrite访问过也会，使用请慎重，概率筛到目标组件数据没变过的实体
        //eq.SetChangedVersionFilter(ComponentType.ReadWrite<TestComponentData>());
    }
    /// <summary>
    /// 该函数相当于MonoBehaviour中的Update，每帧调用，默认是和Update同一个时机，不过也可以修改成PrelateUpdate同一个时机等，讲系统组时会讲到
    /// </summary>
    protected override void OnUpdate()
    {
        Update2();
    }
    /// <summary>
    /// 第一种读取和修改获取的组件的数据的方法
    /// </summary>
    private void Update1(){
        //这串方法贼长而且难以理解，Entities的API挺复杂混乱的，所以我只讲推荐的方法
        //With传入一个eq，这个类会自动帮你调用eq的获取组件函数并处理后传给你，原理详见创建方法2
        //foreach的意思是迭代所有匹配到的实体，Entity e这行参数可以删了也可以留着，看你处理时需不需要这个信息了
        //组件前缀必须用ref，因为是组件是个struct，不用ref你修改的数据没法保存~
        //该方法只能改IComponentData，不过是同步运行的，用e和EntityManager还是有操作空间的
        this.Entities.With(eq).ForEach<TestComponentData>((Entity e,ref TestComponentData data)=>{
            data.Value += 1;
            //举个操作空间的例子
            var data2 = EntityManager.GetSharedComponentData<TestSharedComponentData>(e);
        });
    }
    /// <summary>
    /// 第二种读取和修改获取的组件的数据的方法
    /// </summary>
    private void Update2(){
        //通过eq直接获取搜到的组件的数组
        //NativeArray是Unity官方推出的非托管的数组，注意非托管数组是手动释放，用完要dispose
        //位于Unity.Collections命名空间中，相关的NativeContainer类型以后会讲
        //Allocator标志着这个数组该什么时候释放，如果你没在规定时间内释放会报错提醒你，避免了内存泄露，TempJob意为要在几帧内释放
        //（当然具体报错时机我也不知道，按照规则用就是了）
        NativeArray<TestComponentData> dataarray = eq.ToComponentDataArray<TestComponentData>(Allocator.TempJob);
        //获得entity的数组，该数组的index与dataarray一一对应，
        NativeArray<Entity> entities = eq.ToEntityArray(Allocator.TempJob);
        //eq也可以获取ComponentObject，但SharedCompoenntData和buffer只能通过EntityManager获取，如上一个方法里的操作
        for(int i = 0;i<entities.Length;i++){
            var e = entities[i];
            var data = dataarray[i];
            data.Value += 1;
            //在ComponentSystem中应使用该方法来保存数据改动，而不是EntityManager，在JobComponentSystem中则是另外一个相似的方法
            //PostUpdateCommands是一个EntityCommandBuffer对象，可以视为EntityManager的延迟同步版本
            //有关于EntityCommandBuffer以后会细讲
            this.PostUpdateCommands.SetComponent(e,data);//上一个数据修改方法也可以用PostUpdateCommands
        }
        //还有一种不通过PostUpdateCommands的保存方式，将数据修改存回原本的数组dataarray，然后调用该方法
        //eq.CopyFromComponentDataArray(dataarray);

        //释放
        dataarray.Dispose();
        entities.Dispose();
    }
}
