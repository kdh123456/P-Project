using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class EnemyCtrl : MonoBehaviour
{
	//�ذ� ����
	public enum SkullState { None, Idle, Move, Wait, GoTarget, Atk, Damage, Die }

	//�ذ� �⺻ �Ӽ�
	[Header("�⺻ �Ӽ�")]
	//�ذ� �ʱ� ����
	public SkullState skullState = SkullState.None;
	//�ذ� �̵� �ӵ�
	public float spdMove = 1f;
	//�ذ��� �� Ÿ��
	public GameObject targetCharactor = null;
	//�ذ��� �� Ÿ�� ��ġ���� (�Ź� �� ã������)
	public Transform targetTransform = null;
	//�ذ��� �� Ÿ�� ��ġ(�Ź� �� ã����)
	public Vector3 posTarget = Vector3.zero;

	//�ذ� �ִϸ��̼� ������Ʈ ĳ�� 
	private Animator ani = null;
	//�ذ� Ʈ������ ������Ʈ ĳ��
	private Transform skullTransform = null;

	private int count = 0;

	[Header("�����Ӽ�")]
	//�ذ� ü��
	public int hp = 100;
	public int atk = 50;
	//�ذ� ���� �Ÿ�
	public float AtkRange = 1.5f;
	public ParticleSystem[] attackparticle;//�ذ� �ǰ� ����Ʈ
	public float radius;
	public LayerMask layerMask;
	public GameObject effectDamage = null;
	//�ذ� ���� ����Ʈ
	public GameObject effectDie = null;

	private SkinnedMeshRenderer skinnedMeshRenderer = null;

	EventParam eventParam;
	EventParam eventParam2;
	// Start is called before the first frame update
	void Start()
	{
		//ó�� ���� ������
		skullState = SkullState.Idle;

		//�ִϸ���, Ʈ������ ������Ʈ ĳ�� : �������� ã�� ������ �ʰ�
		ani = GetComponent<Animator>();
		skullTransform = GetComponent<Transform>();

		//��Ų�Ž� ĳ��
		skinnedMeshRenderer = skullTransform.Find("SoldierRen").GetComponent<SkinnedMeshRenderer>();
		EventManager.StartListening("Attaking", IsAttack);
		for(int i = 0; i< attackparticle.Length; i++)
		{
			attackparticle[i].Pause();
		}
		effectDamage.GetComponent<ParticleSystem>().Pause();
		effectDie.GetComponent<ParticleSystem>().Pause();
	}

	void OnAtkAnmationFinished()
	{
		ani.SetBool("isWalk", false);
		ani.SetBool("isDie", false);
		ani.SetBool("isDamage", false);
		ani.SetBool("isAttack", false);
		skullState = SkullState.Idle;
		theAtk();
	}

	void OnDmgAnmationFinished()
	{
		ani.SetBool("isWalk", false);
		ani.SetBool("isDie", false);
		ani.SetBool("isDamage", false);
		ani.SetBool("isAttack", false);
		skullState = SkullState.Idle;
		effectDamage.GetComponent<ParticleSystem>().Pause();
		effectDie.GetComponent<ParticleSystem>().Pause();
		effectDamage.GetComponent<ParticleSystem>().Clear();
		effectDie.GetComponent<ParticleSystem>().Clear();
	}

	void OnDieAnmationFinished()
	{
		//���� ���� 
		gameObject.SetActive(false);
	}

	/// <summary>
	/// �ִϸ��̼� �̺�Ʈ�� �߰����ִ� ��. 
	/// </summary>
	/// <param name="clip">�ִϸ��̼� Ŭ�� </param>
	/// <param name="funcName">�Լ��� </param>
	void OnAnimationEvent(AnimationClip clip, string funcName)
	{
		//�ִϸ��̼� �̺�Ʈ�� ����� �ش�
		AnimationEvent retEvent = new AnimationEvent();
		//�ִϸ��̼� �̺�Ʈ�� ȣ�� ��ų �Լ���
		retEvent.functionName = funcName;
		//�ִϸ��̼� Ŭ�� ������ �ٷ� ������ ȣ��
		retEvent.time = clip.length - 0.1f;
		//�� ������ �̺�Ʈ�� �߰� �Ͽ���
		clip.AddEvent(retEvent);
	}



	/// <summary>
	/// �ذ� ���¿� ���� ������ �����ϴ� �Լ� 
	/// </summary>
	void CkState()
	{
		switch (skullState)
		{
			case SkullState.Idle:
				//�̵��� ���õ� RayCast��
				setIdle();
				break;
			case SkullState.GoTarget:
			case SkullState.Move:
				setMove();
				break;
			case SkullState.Atk:
				setAtk();
				break;
			default:
				break;
		}
	}

	// Update is called once per frame
	void Update()
	{
		CkState();
		AnimationCtrl();
	}

	/// <summary>
	/// �ذ� ���°� ��� �� �� ���� 
	/// </summary>
	void setIdle()
	{
		if (targetCharactor == null)
		{
			posTarget = new Vector3(skullTransform.position.x + Random.Range(-10f, 10f),
									skullTransform.position.y + 1000f,
									skullTransform.position.z + Random.Range(-10f, 10f)
				);
			Ray ray = new Ray(posTarget, Vector3.down);
			RaycastHit infoRayCast = new RaycastHit();
			if (Physics.Raycast(ray, out infoRayCast, Mathf.Infinity) == true)
			{
				posTarget.y = infoRayCast.point.y;
			}
			skullState = SkullState.Move;
		}
		else
		{
			skullState = SkullState.GoTarget;
		}
	}

	/// <summary>
	/// �ذ� ���°� �̵� �� �� �� 
	/// </summary>
	void setMove()
	{
		//����� ������ �� ������ ���� 
		Vector3 distance = Vector3.zero;
		//��� ������ �ٶ󺸰� ���� �ִ��� 
		Vector3 posLookAt = Vector3.zero;

		//�ذ� ����
		switch (skullState)
		{
			//�ذ��� ���ƴٴϴ� ���
			case SkullState.Move:
				//���� ���� ��ġ ���� ���ΰ� �ƴϸ�
				if (posTarget != Vector3.zero)
				{
					//��ǥ ��ġ���� �ذ� �ִ� ��ġ ���� ���ϰ�
					distance = posTarget - skullTransform.position;

					//���࿡ �����̴� ���� �ذ��� ��ǥ�� �� ���� ���� ���� 
					if (distance.magnitude < AtkRange)
					{
						//��� ���� �Լ��� ȣ��
						StartCoroutine(setWait());
						//���⼭ ����
						return;
					}

					//��� ������ �ٶ� �� ����. ���� ����
					posLookAt = new Vector3(posTarget.x,
											//Ÿ���� ���� ���� ��찡 ������ y�� üũ
											skullTransform.position.y,
											posTarget.z);
				}
				break;
			//ĳ���͸� ���ؼ� ���� ���ƴٴϴ�  ���
			case SkullState.GoTarget:
				//��ǥ ĳ���Ͱ� ���� ��
				if (targetCharactor != null)
				{
					//��ǥ ��ġ���� �ذ� �ִ� ��ġ ���� ���ϰ�
					distance = targetCharactor.transform.position - skullTransform.position;
					//���࿡ �����̴� ���� �ذ��� ��ǥ�� �� ���� ���� ���� 
					if (distance.magnitude < AtkRange)
					{
						//���ݻ��·� �����մ�.
						skullState = SkullState.Atk;
						//���⼭ ����
						return;
					}
					//��� ������ �ٶ� �� ����. ���� ����
					posLookAt = new Vector3(targetCharactor.transform.position.x,
											//Ÿ���� ���� ���� ��찡 ������ y�� üũ
											skullTransform.position.y,
											targetCharactor.transform.position.z);
				}
				break;
			default:
				break;

		}

		//�ذ� �̵��� ���⿡ ũ�⸦ ���ְ� ���⸸ ����(normalized)
		Vector3 direction = distance.normalized;

		//������ x,z ��� y�� ���� �İ� ���Ŷ� ����
		direction = new Vector3(direction.x, 0f, direction.z);

		//�̵��� ���� ���ϱ�
		Vector3 amount = direction * spdMove * Time.deltaTime;

		//ĳ���� ��Ʈ���� �ƴ� Ʈ���������� ���� ��ǥ �̿��Ͽ� �̵�
		skullTransform.Translate(amount, Space.World);
		//ĳ���� ���� ���ϱ�
		skullTransform.LookAt(posLookAt);
	}

	/// <summary>
	/// ��� ���� ���� �� 
	/// </summary>
	/// <returns></returns>
	IEnumerator setWait()
	{
		//�ذ� ���¸� ��� ���·� �ٲ�
		skullState = SkullState.Wait;
		//����ϴ� �ð��� �������� �ʰ� ����
		float timeWait = Random.Range(1f, 3f);
		//��� �ð��� �־� ��.
		yield return new WaitForSeconds(timeWait);
		//��� �� �ٽ� �غ� ���·� ����
		skullState = SkullState.Idle;
	}

	/// <summary>
	/// �ִϸ��̼��� ��������ִ� �� 
	/// </summary>
	void AnimationCtrl()
	{
		//�ذ��� ���¿� ���� �ִϸ��̼� ����
		switch (skullState)
		{
			//���� �غ��� �� �ִϸ��̼� ��.
			case SkullState.Wait:
			case SkullState.Idle:
				//�غ� �ִϸ��̼� ����
				ani.SetBool("isAttack", false);
				ani.SetBool("isWalk", false);
				ani.SetBool("isDie", false);
				ani.SetBool("isDamage", false);
				break;
			//������ ��ǥ �̵��� �� �ִϸ��̼� ��.
			case SkullState.Move:
			case SkullState.GoTarget:
				//�̵� �ִϸ��̼� ����
				ani.SetBool("isWalk", true);
				ani.SetBool("isDie", false);
				ani.SetBool("isDamage", false);
				ani.SetBool("isAttack", false);
				break;
			//������ ��
			case SkullState.Atk:
				//���� �ִϸ��̼� ����
				ani.SetBool("isWalk", true);
				ani.SetBool("isDie", false);
				ani.SetBool("isDamage", false);
				ani.SetBool("isAttack", true);
				break;
			//�׾��� ��
			case SkullState.Die:
				//���� ���� �ִϸ��̼� ����
				ani.SetBool("isDie", true);
				ani.SetBool("isDamage", false);
				ani.SetBool("isAttack", false);
				ani.SetBool("isWalk", false);
				break;
			case SkullState.Damage:
				ani.SetBool("isDie", false);
				ani.SetBool("isDamage", true);
				ani.SetBool("isAttack", false);
				ani.SetBool("isWalk", false);
				break;
			default:
				break;

		}
	}

	///<summary>
	///�þ� ���� �ȿ� �ٸ� Trigger �Ǵ� ĳ���Ͱ� ������ ȣ�� �ȴ�.
	///�Լ� ������ ��ǥ���� ������ ��ǥ���� �����ϰ� �ذ��� Ÿ�� ��ġ�� �̵� ��Ų�� 
	///</summary>

	void OnCkTarget(GameObject target)
	{
		//��ǥ ĳ���Ϳ� �Ķ���ͷ� ����� ������Ʈ�� �ְ� 
		targetCharactor = target;
		//��ǥ ��ġ�� ��ǥ ĳ������ ��ġ ���� �ֽ��ϴ�. 
		targetTransform = targetCharactor.transform;

		//��ǥ���� ���� �ذ��� �̵��ϴ� ���·� ����
		skullState = SkullState.GoTarget;

	}

	/// <summary>
	/// �ذ� ���� ���� ���
	/// </summary>
	void setAtk()
	{
		//�ذ�� ĳ���Ͱ��� ��ġ �Ÿ� 
		float distance = Vector3.Distance(targetTransform.position, skullTransform.position); //���̴�

		//���� �Ÿ����� �� ���� �Ÿ��� �־� ���ٸ� 
		if (distance > AtkRange + 0.5f)
		{
			//Ÿ�ٰ��� �Ÿ��� �־����ٸ� Ÿ������ �̵� 
			skullState = SkullState.GoTarget;
		}
	}

	void theAtk()
	{
		for (int i = 0; i < attackparticle.Length; i++)
		{
			attackparticle[i].Clear();
			attackparticle[i].Play();
		}
		Collider[] a = Physics.OverlapSphere(this.transform.position, radius, layerMask);

		if (a.Length > 0)
		{
			Debug.Log("?");
			eventParam2.eventint = atk;
			EventManager.TriggerEvent("PlayerDamage", eventParam2);
		}
	}


	/// <summary>
	/// �ذ� �ǰ� �浹 ���� 
	/// </summary>
	/// <param name="other"></param>
	private void OnTriggerEnter(Collider other)
	{
		//���࿡ �ذ��� ĳ���� ���ݿ� �¾Ҵٸ�
		if (other.gameObject.CompareTag("PlayerAtk") == true && count < eventParam.eventint)
		{
			count++;
			//�ذ� ü���� 10 ���� 
			hp -= 10;
			if (hp > 0)
			{
				//�ǰ� ����Ʈ 
				effectDamage.GetComponent<ParticleSystem>().Play();

				effectDamageTween();
				//ü���� 0 �̻��̸� �ǰ� �ִϸ��̼��� ���� �ϰ� 
				skullState = SkullState.Damage;
			}
			else
			{
				//0 ���� ������ �ذ��� ���� ���·� �ٲپ��  
				skullState = SkullState.Die;
				effectDie.GetComponent<ParticleSystem>().Play();
			}
		}
	}

	/// <summary>
	/// �ǰݽ� ���� ������ ��½��½ ȿ���� �ش�
	/// </summary>
	void effectDamageTween()
	{
		//Ʈ���� ������ �� Ʈ�� �Լ��� ����Ǹ� ������ ������ �� �� �־ 
		//Ʈ�� �ߺ� üũ�� �̸� ������ ���ش�

		//��½�̴� ����Ʈ ������ �������ش�
		Color colorTo = Color.red;

		skinnedMeshRenderer.material.DOColor(colorTo, 0f).OnComplete(OnDamageTweenFinished);
	}

	/// <summary>
	/// �ǰ�����Ʈ ����� �̺�Ʈ �Լ� ȣ��
	/// </summary>
	void OnDamageTweenFinished()
	{
		//Ʈ���� ������ �Ͼ������ Ȯ���� ������ �����ش�
		skinnedMeshRenderer.material.DOColor(Color.white, 2f);
	}

	void IsAttack(EventParam events)
	{
		eventParam = events;
		if (eventParam.eventint == 0)
		{
			count = 0;
		}
	}
}