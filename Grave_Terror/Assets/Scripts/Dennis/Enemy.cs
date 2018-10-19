﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum E_STATE
{
	NORMAL,
	IGNITED
}

[RequireComponent(typeof(CharacterController))]
public class Enemy : MonoBehaviour
{
	[HideInInspector] //THe spawner that created us
	public Spawner spawner;

	private EnemyHealth health;

	public GameObject target;

	private CharacterController cc;

	[HideInInspector] //Current state
	public E_STATE state = E_STATE.NORMAL;

	//Is this actor alive?
	[HideInInspector]
	public bool alive = false;

	//Values to be set in the editor
	[Tooltip("Actor attack damage")]
	public int attackDamage;
	[Tooltip("Actor attack range (in unity meters)")]
	public float attackRange;
	[Tooltip("Actor travel speed")]
	public float movementSpeed;

	[HideInInspector]
	public float fireDamageOverTime = 0.0f;

	//Public so the controller can easily access it
	[HideInInspector]
	public List<BaseBehaviour> behaviours = new List<BaseBehaviour>(4);

	[HideInInspector]
	public Vector3 velocity;
	private Vector3 acceleration;

	public float drag = 0.97f;
	public float mass = 10.0f;

	// Use this for initialization
	void Start ()
	{
		behaviours.Add(new EnemyPathing(this, 0.67f));
		behaviours.Add(new EnemyFlocking(this, 0.33f));
		behaviours.Add(new EnemyWander(this, 0.33f));

		cc = GetComponent<CharacterController>();

		health = GetComponent<EnemyHealth>();
	}
	
	// Update is called once per frame
	void Update ()
	{
		switch (state)
		{
			case E_STATE.NORMAL:
				break;

			case E_STATE.IGNITED:
				health.DamageHealth(fireDamageOverTime * Time.deltaTime);
				break;
		}

		//Cycle through all behaviours
		acceleration = Vector3.zero;
		foreach (BaseBehaviour b in behaviours)
		{
			acceleration += b.Update() * b.weight;
		}
		//No up velocity
		acceleration.y = 0.0f;
		//Normalize it and multiply it by our speed
		acceleration.Normalize();
		acceleration *= movementSpeed;

		velocity += acceleration / mass;
		velocity *= drag;

		//rotate towards where we are going
		if(velocity != Vector3.zero)
			transform.rotation.SetLookRotation(velocity);

		//Cap max velocity
		if (velocity.magnitude > movementSpeed)
		{
			velocity = Vector3.Normalize(velocity) * movementSpeed;
		}
	}

	private void FixedUpdate()
	{
		cc.Move(velocity * Time.deltaTime);
	}

	//Newly spawned or just respawned
	public void Init()
	{
		alive = true;
		//Tell the EnemyController we are alive
		//EnemyController.instance.RegisterEnemy(this);
	}
	//Actor died
	public void Dead()
	{
		//Prevents this from being called twice
		if (alive)
		{
			alive = false;

			//EnemyController.instance.DeregisterEnemy(this);
			spawner.Despawn(gameObject);
		}
	}

	public void ResetBehaviours()
	{
		foreach (BaseBehaviour b in behaviours)
		{
			b.Reset();
		}
	}

	//Ignite this enemy
	public void Ignite(float _fire_strength = 10.0f)
	{
		state = E_STATE.IGNITED;
		fireDamageOverTime = _fire_strength;
	}
}
