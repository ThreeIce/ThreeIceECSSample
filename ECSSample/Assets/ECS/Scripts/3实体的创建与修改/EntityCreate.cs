

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;



//前言：
//离上一次ECS教程更新过去了很久了，ECS飞速迭代，十一月中旬推出了0.2，十一月底（又或是十二月初）推出了0.3
//变化极其之大，0.1到0.2淘汰了一堆东西，又新增了一堆东西（但是0.2到0.3只增加了一个功能，官方在想啥）
//举个例子，以前通过World.Active可以获得运行的一个World，现在被废弃了，改成了World.AllWorlds（感觉是个好事）
//注意，0.3只支持2019.3f








/// <summary>
/// 最基本的组件类型
/// </summary>
public struct TestComponentData : IComponentData
{
    public int Value;
}
/// <summary>
/// 组件数据共享的组件类型，每个entity只拥有一个引用，一些较大的重复性数据用这个就不需要每个Entity都存了
/// </summary>
public struct TestSharedComponentData : ISharedComponentData
{
    public int Value;
}
/// <summary>
/// Entity的集合组件类型
/// </summary>
public struct TestBufferElementData : IBufferElementData
{
    public int Value;
}
/// <summary>
/// 特殊的ComponentData，具有在Entity删除时保留下来的特性
/// </summary>
//SharedComponentData和BufferElementData也有SystemState版本，操作方法类似不多讲了
public struct TestSystemStateComponentData : ISystemStateComponentData
{
    public int Value;
}

public class TestClass : IComponentData
{
    public int Value = 1;
}

public class EntityCreate : MonoBehaviour
{
    public World world;
    public EntityManager EntityManager;
    // Start is called before the first frame update
    void Start()
    {
        //由于现在在MonoBehaviour里执行代码，MonoBehaviour并不属于任何一个世界，没有对任何一个世界的引用
        //所以必须手动选择一个要操作的世界，在这个世界中生成Entity
        world = World.AllWorlds[0];//默认情况下，unity会在开始运行时自动给我们创建一个默认世界
        //没创建其它世界的话这个列表里就只会有这一个世界。
        EntityManager = world.EntityManager;//获得该世界的EntityManager，通过EntityManager对世界中的实体操作
        Create1();
        Entity e = Create4(Create3());
        GetAndSet(e);
    }
    /// <summary>
    /// 第一种创建entity的方式
    /// </summary>
    public Entity Create1()
    {
        //通过EntityArchetype创建Entity
        //EntityArchetype类似于没有具体组件数值的Prefab，通过它生成的实体的组件的值都是结构体的默认数值
        //EntityArchetype的创建需要指定该Archetype有什么组件类型，组件的类型可以通过ComponentType.ReadWrite获取
        EntityArchetype ea = EntityManager.CreateArchetype(ComponentType.ReadWrite<TestComponentData>(),
            ComponentType.ReadWrite<TestSharedComponentData>(),
            ComponentType.ReadWrite<TestBufferElementData>(),
            ComponentType.ReadWrite<TestSystemStateComponentData>());
        //此处使用ReadWrite和ReadOnly没有区别，但不清楚以后会不会有区别，推荐都使用readwrite
        return EntityManager.CreateEntity(ea);
    }
    /// <summary>
    /// 第二种创建entity的方式
    /// </summary>
    public Entity Create2()
    {
        //直接通过ComponentType创建Entity，EntityManager会在内部自己创建一个Archetype
        return EntityManager.CreateEntity(ComponentType.ReadWrite<TestComponentData>(),
            ComponentType.ReadWrite<TestSharedComponentData>(),
            ComponentType.ReadWrite<TestBufferElementData>(),
            ComponentType.ReadWrite<TestSystemStateComponentData>());
    }
    /// <summary>
    /// 第三种创建方式，兼如何往一个实体上添加组件
    /// </summary>
    public Entity Create3()
    {
        //创建一个空实体，并往其上添加组件
        Entity e = EntityManager.CreateEntity();
        //这种方法可以往实体上添加设定好数值的组件，但是速度比其他方法会稍慢一些，推荐配合第四个方法使用
        EntityManager.AddComponentData<TestComponentData>(e, new TestComponentData() { Value = 1 });
        //这里的泛型由于类型推测是可以省略的，我就不打了
        EntityManager.AddSharedComponentData(e, new TestSharedComponentData() { Value = 1 });
        //ISystemStateComponentData继承自IComponentData，所以直接把它当作ComponentData用就行了，只是有些特性
        EntityManager.AddComponentData(e, new TestSystemStateComponentData() { Value = 1 });
        //上面三个函数返回值为bool，bool的意思是如果目标实体上没有同类型组件，那么返回true并添加该组件到实体上
        //如果有，返回false并不进行操作。ECS是不支持同个Entity拥有多个同类型组件的

        //Buffer的创建比较特殊
        DynamicBuffer<TestBufferElementData> buffer = EntityManager.AddBuffer<TestBufferElementData>(e);
        //可以通过该函数往buffer里加入内容
        buffer.Add(new TestBufferElementData { Value = 1 });
        //Buffer内容的删除也很特殊，官方只支持了删除指定index的对应内容的函数
        //如buffer.RemoveAt(0);
        //Buffer可以当成普通List来用
        Debug.Log(buffer[0].Value);

        //在这里顺便补充个特殊组件类型，第一个视频里没讲，因为它其实并不是ecs应该有的功能
        //是为了兼容GameObject做的妥协，用这玩意性能会偏差，但总体比GameObject会好一些，最好不要多用
        //这个函数的泛型支持继承自object的所有类，但是经过实测，需要使类被注册到unity ECS的一个TypeManager里
        //在0.3里最方便的方法是使这个类继承自IComponentData，另外继承自MonoBehaviour的一切类都是支持的
        EntityManager.AddComponentObject(e,new TestClass());
        return e;
    }
    /// <summary>
    /// 第四种创建方法
    /// </summary>
    public Entity Create4(Entity Prefab)
    {
        //可以复制一个现有实体的所有组件数据来创建一个新实体，和GameObject.Instantiate差不多
        return EntityManager.Instantiate(Prefab);
        //为何说配合第三种方法好呢，先用第三种方法创建一个作为Prefab的实体，再通过这种方法大量复制，会很方便
        //但是要注意，SystemState类型的组件不会被复制
        //因为官方希望SystemState是在运行时由系统添加的而不是在创建时添加的，具体为什么以后讲
        //如果希望作为Prefab的实体不参与游戏运行，而像GameObject工作流程中的Prefab一样独立的话
        //可以在该实体上添加Prefab组件，就不会被任何系统所访问，prefab和disable的特殊地位以后讲
    }
    /// <summary>
    /// 通过EntityManager获取和保存组件的值，这些方法不是很推荐，如在系统中应使用另一套方法
    /// </summary>
    /// <param name="e"></param>
    public void GetAndSet(Entity e)
    {
        //获取普通组件，SystemState组件同理
        TestComponentData data1 = EntityManager.GetComponentData<TestComponentData>(e);
        data1.Value += 1;
        //保存组件数据的修改，同理这里的泛型可省略
        EntityManager.SetComponentData<TestComponentData>(e,data1);
        //获取共享组件
        TestSharedComponentData data2 = EntityManager.GetSharedComponentData<TestSharedComponentData>(e);
        data2.Value += 1;
        //保存组件数据的修改，注意这里只会使该entity对应另外一个共享组件，而不会修改entity对应共享组件的值
        EntityManager.SetSharedComponentData(e, data2);

        //获取Buffer组件，修改不需要调用函数保存，会直接保存
        DynamicBuffer<TestBufferElementData> data3 = EntityManager.GetBuffer<TestBufferElementData>(e);
        //修改方式已经介绍过了，就不再介绍了

        //获取ComponentObject组件
        TestClass data4 = EntityManager.GetComponentObject<TestClass>(e);
        data4.Value += 1;//ComponentObject和Buffer一样是自动保存的
        
    }
    /// <summary>
    /// Entity的删除
    /// </summary>
    public void Delete(Entity e)
    {
    //本函数仅供介绍，不准备运行，所以上下文没有关联，每段代码都可以拆出去用，但直接运行会报错，可以猜猜为什么
        EntityManager.DestroyEntity(e);
        //删除组件的函数只有一个，什么类型的组件都可以用这个函数删除
        EntityManager.RemoveComponent<TestComponentData>(e);
        EntityManager.RemoveComponent<TestSharedComponentData>(e);
        EntityManager.RemoveComponent<TestSystemStateComponentData>(e);
        EntityManager.RemoveComponent<TestBufferElementData>(e);

    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
