﻿using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Child : Character {
	public float allowedCliffSpace = 0.1f;
	protected override void Start() {
		base.Start();
		stateMachine = new CharacterStateMachine(this, States.Child_Air);
		isFollowHero = true;
	}

	protected override void FixedUpdate() {
		base.FixedUpdate();
	}


	[HideInInspector] public bool isFollowHero;
	/// <summary>
	/// true를 넣으면 따라가고, false는 정지합니다.
	/// </summary>
	public void SetFollow(bool isFollow) {
		isFollowHero = isFollow;
	}
}
public class ChildState : CharacterState {
	public Child child;
	public Hero hero;
	protected InputManager input;
	public override void Init() {
		base.Init();
		child = character.GetChildClass<Child>();
		hero = GameManager.Instance.hero;
		input = InputManager.Instance;
		moveStat = unit.status;
	}
}

public class ChildGround : ChildState {

	float heroXRand;
	float timer;
	float safetyDistance;
	public override void Enter() {
		moveStat.verticalSpeed = 0;
		timer = 0;
		safetyDistance = Random.RandomRange(0.5f, 1.5f);//추락지점 이격거리
	}
	public override void Execute() {
		
		Vector2 groundForward = Vector2.right;

		//지형 부착
		if (unit.AttachGround()) {
			groundForward = unit.groundForward;
		} else {
			sm.SetState(States.Child_Air);
		}

		//아이들끼리 약간 분산시키는 값
		timer -= Time.deltaTime;
		if (timer < 0) {
			timer = Random.Range(0.3f, 2f);
			heroXRand = Random.Range(-2, 2);
		}

		//좌우이동 인공지능
		int moveDir = 0;
		float followStartDist = 0.3f;//이동 시작하는 최소값

		//범위 안에 들어와있고 캐릭터 추적모드일때만 이동
		if (unit.IsInSensor(hero.unit.transform.position) && child.isFollowHero) {
			float heroXDist = hero.unit.transform.position.x - unit.transform.position.x;
			heroXDist += heroXRand;//아이들끼리 약간 분산시키기
			int heroDir = Mathf.Abs(heroXDist) >= followStartDist ? (heroXDist > 0 ? 1 : -1) : 0;
			float dist = unit.GetWalkableDistance(heroDir, safetyDistance*1.5f, child.allowedCliffSpace);
			moveDir = dist >= safetyDistance ? heroDir : 0;
		}


		
		//이동호출
		bool isWallForward = unit.HandleMoveSpeed(moveDir, moveStat.groundMoveSpeed);
		Vector2 vel = groundForward * moveStat.sideMoveSpeed;		
		unit.SetMovement(MovementType.SetVelocity, vel);

		//애니메이션
		if(moveDir != 0 && !isWallForward) {
			animator.SetBool("IsWalking", true);
			animator.GetComponent<SpriteRenderer>().flipX = moveDir > 0 ? false : true;
		} else {
			animator.SetBool("IsWalking", false);
		}

	}
}

public class ChildAir : ChildState {
	public override void Enter() {
		animator.SetBool("IsWalking", false);
	}
	public override void Execute() {

		
		//좌우이동
		unit.HandleMoveSpeed(0, moveStat.airMoveSpeed);

		//추락
		moveStat.verticalSpeed -= moveStat.fallSpeed;

		//이동호출
		Vector2 vel = new Vector2(moveStat.sideMoveSpeed, moveStat.verticalSpeed);
		unit.SetMovement(MovementType.SetVelocity, vel);


		//착지판정
		if (unit.GroundCheckFromAir())
			sm.SetState(States.Child_Ground);
	}

}
