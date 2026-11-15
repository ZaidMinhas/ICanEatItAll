using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class NPCMovementTests
{
    private readonly List<GameObject> objects = new List<GameObject>();
    private NavMeshData mesh;
    private NavMeshDataInstance meshInstance;

    // Gameplay scripts live in Unity's default assembly, which test assemblies cannot reference.
    private static readonly Type NPCType = Type.GetType("NPCMovement, Assembly-CSharp");
    private static readonly Type FoodType = Type.GetType("EdibleItem, Assembly-CSharp");

    [SetUp]
    public void SetUp()
    {
        var floor = new NavMeshBuildSource
        {
            shape = NavMeshBuildSourceShape.Box,
            size = new Vector3(40, 0.2f, 40),
            transform = Matrix4x4.TRS(new Vector3(0, -0.1f, 0), Quaternion.identity, Vector3.one)
        };
        mesh = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByIndex(0),
            new List<NavMeshBuildSource> { floor }, new Bounds(Vector3.zero, new Vector3(40, 10, 40)),
            Vector3.zero, Quaternion.identity);
        Assert.That(mesh, Is.Not.Null);
        meshInstance = NavMesh.AddNavMeshData(mesh);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject item in objects) if (item != null) Object.DestroyImmediate(item);
        objects.Clear();
        meshInstance.Remove();
        Object.DestroyImmediate(mesh);
    }

    private GameObject CreateObject(string name, Vector3 position)
    {
        var item = new GameObject(name);
        item.transform.position = position;
        objects.Add(item);
        return item;
    }

    private GameObject Food(float x)
    {
        var food = CreateObject("Food", new Vector3(x, 0, 0));
        food.tag = "NPCTarget";
        food.AddComponent(FoodType);
        return food;
    }

    private Component NPC(string state, Transform player = null)
    {
        var npc = CreateObject("NPC", Vector3.zero);
        npc.SetActive(false);
        var agent = npc.AddComponent<NavMeshAgent>();
        agent.speed = 15;
        agent.acceleration = 100;
        agent.angularSpeed = 720;
        agent.stoppingDistance = 0.1f;
        Component movement = npc.AddComponent(NPCType);
        FieldInfo stateField = NPCType.GetField("currentState");
        stateField.SetValue(movement, Enum.Parse(stateField.FieldType, state));
        NPCType.GetField("player").SetValue(movement, player);
        NPCType.GetField("stealingDistance").SetValue(movement, 0.7f);
        npc.SetActive(true);
        return movement;
    }

    private static IEnumerator WaitUntilOrTimeout(Func<bool> condition)
    {
        float deadline = Time.realtimeSinceStartup + 5f;
        while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(condition(), Is.True, "NPC did not finish the expected movement within five seconds.");
    }

    [UnityTest]
    public IEnumerator PatrolVisitsPointsAndLoopsBack()
    {
        Component npc = NPC("Patrol");
        NPCType.GetField("patrolPoints", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(npc, new List<Vector3> { new Vector3(3, 0, 0), new Vector3(6, 0, 0) });
        yield return WaitUntilOrTimeout(() => npc.transform.position.x >= 5.4f);
        yield return WaitUntilOrTimeout(() => npc.transform.position.x <= 3.5f);
    }

    [UnityTest]
    public IEnumerator FoodSearchResumesAfterSwitchingToFleeAndBack()
    {
        GameObject food = Food(6);
        GameObject player = CreateObject("Player", Vector3.right);
        Component npc = NPC("FindFood", player.transform);
        yield return null;
        FieldInfo stateField = NPCType.GetField("currentState");
        stateField.SetValue(npc, Enum.Parse(stateField.FieldType, "Flee"));
        yield return null;
        stateField.SetValue(npc, Enum.Parse(stateField.FieldType, "FindFood"));
        yield return WaitUntilOrTimeout(() => food == null);
    }

    [UnityTest]
    public IEnumerator FindFoodStealsNearestThenNextFood()
    {
        GameObject first = Food(3);
        GameObject second = Food(6);
        Component npc = NPC("FindFood");
        yield return null;
        Assert.That(NPCType.GetField("targetFood", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(npc), Is.EqualTo(first));
        yield return WaitUntilOrTimeout(() => first == null);
        yield return WaitUntilOrTimeout(() => second == null);
    }

    [UnityTest]
    public IEnumerator FindFoodRecoversWhenPlayerTakesItsTarget()
    {
        GameObject first = Food(3);
        GameObject second = Food(6);
        NPC("FindFood");
        yield return null;
        Object.Destroy(first);
        yield return WaitUntilOrTimeout(() => second == null);
    }

    [UnityTest]
    public IEnumerator FindFoodWaitsForNewFoodAndIgnoresNonEdibleTargets()
    {
        GameObject decoration = CreateObject("Not food", Vector3.right);
        decoration.tag = "NPCTarget";
        NPC("FindFood");
        yield return null;
        yield return null;
        GameObject food = Food(3);
        yield return WaitUntilOrTimeout(() => food == null);
        Assert.That(decoration != null, Is.True);
    }

    [UnityTest]
    public IEnumerator FleeMovesAwayAndStopsAtSafeDistance()
    {
        GameObject player = CreateObject("Player", Vector3.right);
        Component npc = NPC("Flee", player.transform);
        yield return WaitUntilOrTimeout(() => Vector3.Distance(npc.transform.position, player.transform.position) >= 5f);
        yield return new WaitForSeconds(0.2f);
        Assert.That(npc.transform.position.x, Is.LessThan(-3f));
        Assert.That(npc.GetComponent<NavMeshAgent>().hasPath, Is.False);
    }
}
