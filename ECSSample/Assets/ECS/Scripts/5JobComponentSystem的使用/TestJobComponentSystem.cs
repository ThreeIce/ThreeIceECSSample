//Entities最近更新飞快，前段时间就已经更新到了0.7，增加和修改了很多内容（录视频的时候已经0.8了，暂时有不明bug，本教程基于0.7进行）
//其中最为主要的就是Unity对ComponentSystem.Entites的大力支持，增加并改进了许多的功能，原本我比较抗拒，现在用它来修改组件才是最方便的道路
//Entities具体的使用方法将在下个视频介绍，今天将讲如何使用Jobs在Unity ECS中异步执行代码
//Unity最大的一个痛处就是无法利用多核CPU，游戏只能单线程运行，但是，在ECS+Jobs下，性能问题，就不再是问题了，很多计算任务都可以交给其它cpu执行！
//想要用Job运行ECS代码，需要JobComponentSystem，它和ComponentSystem有些类似但又有所不同，他们都继承自ComponentSystemBase，都可以使用Entities类
//原本Jobs用不了ComponentSystemBase.Entities，写法区别比较大，现在基本变得很相似了，只是主线程不能给Job传递引用类型
//如果要开启Burst优化性能，也不能在Job中创建引用类型，并且不能调用外部静态方法
//ComponentSystemBase.Entites可以放心大胆的用，不必担心它因为像lambda而产生大量的gc垃圾，Unity底层进行了代码生成，使它不会产生GC
//接下来我将用JobComponentSystem实现和上个教程相同的功能
using Unity.Entities;
//Collecitons命名空间是一些Unity实现的手动管理内存释放的集合类型，供Jobs使用
using Unity.Collections;
//Jobs相关内容所在的命名空间
using Unity.Jobs;
public class TestJobComponentSystem : JobComponentSystem
{
    //部分代码我直接复制了第四节的内容，因为都是通用的
    private EntityQuery eq;

    protected override void OnCreate()
    {
        CreateEntityQuery();
    }
    /// <summary>
    /// 普通eq的创建
    /// </summary>
    private void CreateEntityQuery()
    {
        //由于Jobs不支持访问SharedComponentData，所以ShareComponentData的读取被删了，现在ComponentSystem.Entities支持读取DynamicBuffer
        //所以将DynamicBuffer的访问和修改也纳入示例中，该访问和修改方法普通ComponentSystem可用
        eq = this.GetEntityQuery(ComponentType.ReadWrite<TestComponentData>(), ComponentType.ReadWrite<TestBufferElementData>());
        this.RequireForUpdate(eq);
    }
    //JobComponentSystem和ComponentSystem的第一个区别就在于OnUpdate的输入参数和返回值，ComponentSystem是没有这些东西的
    //在JobComponentSystem中，inputDeps为该系统要执行的Job所依赖的Job，也就是说inputDeps如果没完成该系统的代码不应该执行
    //返回的JobHandle则为该系统执行的Job代码的句柄，JobHandle相当于C#的Task异步系统中async方法返回的ITask
    //JobHandle有一系列检测代码完成状态的方法，相关内容以后讲（也可能不讲，因为Jobs的教程现在是越来越多了）
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        //今天只讲一种JobComponentSystem操作数据的方法，其他方法不再被推荐且需要一定的Job知识，以后有机会再讲
        //这波调用和上一节的几乎一样，不过还是有一些区别，一些区别是因为ECS的更新导致的
        //先说说更新带来的变化，Job系统中的With(eq)改成了WithStoreEntityQueryInField，还要带上ref，普通的系统则不需要改（第四节的代码还能正常运行）
        //Foreach不需要带<>是c#的一个语法糖，能方便很多
        JobHandle handle = this.Entities.WithStoreEntityQueryInField(ref eq)
            .ForEach((Entity e, ref TestComponentData data, ref DynamicBuffer<TestBufferElementData> data2) =>
            {
                data.Value += 1;
                if (data2.Length != 0)
                {
                    var data2data = data2[0];
                    data2data.Value += 1;
                    data2[0] = data2data;
                }
            }).Schedule(inputDeps);//如果只是同步运行这段代码，不需要Schedule函数，如果调用了Schedule函数，就代表这段代码将成为Job被安排执行
        //Schedule的参数是这个Job的依赖项，是一个JobHandle，只有inputDeps指向的Job执行完这个Job才会开始执行。
        //Schedule返回一个JobHandle，就是这段代码的Job的句柄，可以用来让别的Job依赖这段代码。这段代码不执行完成别的代码就不会执行
        return handle;
    }
}

//下节我将细讲Entities的使用，以及Unity最新推出的一个SystemBase与Entities的配合
