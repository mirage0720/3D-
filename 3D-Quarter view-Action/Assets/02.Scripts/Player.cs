using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Player : MonoBehaviour
{
    //public 인 이유 인스펙터창에서 보이기위해서
    public float speed;
    public GameObject[] weapons;
    public bool[] hasWeapons;
    public GameObject[] grenades;
    public int hasGrenades;
    public GameObject grenadeObj;
    public Camera followCamera;
    public GameManager manager;

    public AudioSource jumpSound;
    
    public int ammo;
    public int coin;
    public int health;
    public int score;

    public int maxAmmo;
    public int maxCoin;
    public int maxHealth;
    public int maxHasGrenades;

    float hAxis;
    float vAxis;

    bool wDown;
    bool jDown;
    bool fDown;
    bool gDown;
    bool rDown;
    bool iDown;
    bool sDown1;
    bool sDown2;
    bool sDown3;

    bool isJump;
    bool isDodge;
    bool isSwap;
    bool isReload;
    bool isFireReady = true;
    bool isBorder;
    bool isDamage;
    bool isShop;
    bool isDead;

    Vector3 moveVec;
    Vector3 dodgeVec;

    Rigidbody rigid;
    Animator anim;
    MeshRenderer[] meshs;

    GameObject nearObject;
    public Weapon equipWeapon;
    int equipWeaponIndex = -1;
    float fireDelay;

    void Awake()
    {
        rigid = GetComponent<Rigidbody>();
        anim = GetComponentInChildren<Animator>();
        meshs = GetComponentsInChildren<MeshRenderer>();

        Debug.Log(PlayerPrefs.GetInt("MaxScore"));

        // PlayerPrefs.SetInt("MaxScore", 112500);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    //input manager에서 관리 Horizontal Vertical
    //normalized 어떤 방향이든 1로 보정
    // Update is called once per frame
    void Update()
    {
        GetInput();
        Move();
        Turn();
        Jump();
        Grenade();
        Attack();
        Reload();
        Dodge();
        Swap();
        Interation();
    }

    void GetInput()
    {
        //이동
        hAxis = Input.GetAxisRaw("Horizontal");
        vAxis = Input.GetAxisRaw("Vertical");
        //걷기로 바꾸는 키
        wDown = Input.GetButton("Walk");
        jDown = Input.GetButtonDown("Jump");
        //공격
        fDown = Input.GetButton("Fire1");
        gDown = Input.GetButtonDown("Fire2");
        rDown = Input.GetButtonDown("Reload");
        //아이템
        iDown = Input.GetButtonDown("Interation");
        sDown1 = Input.GetButtonDown("Swap1");
        sDown2 = Input.GetButtonDown("Swap2");
        sDown3 = Input.GetButtonDown("Swap3");
    }

    void Move()
    {
        moveVec = new Vector3(hAxis, 0 , vAxis).normalized;

        if(isDodge){
            moveVec = dodgeVec;
        }

        if(isSwap || !isFireReady || isReload || isDead){
            moveVec = Vector3.zero;
        }

        // 걷기 속도 조절 if문 할때
        // if(wDown){
        //     transform.position += moveVec * speed * 0.3f * Time.deltaTime;
        // }else {
        //     transform.position += moveVec * speed * Time.deltaTime;
        // }

        // 삼항 연산자 사용시
        if(!isBorder){
            transform.position += moveVec * speed * (wDown ? 0.3f : 1f) * Time.deltaTime;
        }

        //달리기 애니메이션 설정
        anim.SetBool("isRun", moveVec != Vector3.zero);
        anim.SetBool("isWalk", wDown);
    }

    void Turn()
    {
        //회전 구현
        //나아가는 방향으로 바라보기 위함
        //키보드 회전
        transform.LookAt( transform.position + moveVec);

        //마우스 회전
        if(fDown && !isDead){
            Ray ray = followCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit rayHit;
            if(Physics.Raycast(ray, out rayHit, 100)){
                Vector3 nextVec = rayHit.point - transform.position;
                nextVec.y = 0;
                transform.LookAt(transform.position + nextVec);
            }
        }
    }

    void Jump()
    {
        //점프키를 눌렀을떄 isJump가 false값이어야만 점프할수있게 구현
        if(jDown && moveVec == Vector3.zero && !isJump && !isDodge && !isDead){
            rigid.AddForce(Vector3.up * 15, ForceMode.Impulse);
            anim.SetBool("isJump", true);
            anim.SetTrigger("doJump");
            isJump = true;

            jumpSound.Play();
        }
    }

    void Grenade()
    {
        if(hasGrenades == 0){
            return;
        }
        if(gDown && !isReload && !isSwap && !isDead){
            Ray ray = followCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit rayHit;
            if(Physics.Raycast(ray, out rayHit, 100)){
                Vector3 nextVec = rayHit.point - transform.position;
                nextVec.y = 15;

                GameObject instantGrenade = Instantiate(grenadeObj, transform.position, transform.rotation);
                Rigidbody rigidGrenade = instantGrenade.GetComponent<Rigidbody>();
                rigidGrenade.AddForce(nextVec, ForceMode.Impulse);
                rigidGrenade.AddTorque(Vector3.back * 10, ForceMode.Impulse);

                hasGrenades --;
                grenades[hasGrenades].SetActive(false);
            }
        }
    }

    void Attack()
    {
        if(equipWeapon == null){
            return;
        }

        fireDelay += Time.deltaTime;
        isFireReady = equipWeapon.rate < fireDelay;

        if(fDown && isFireReady && !isDodge && !isSwap && !isShop && !isDead){
            equipWeapon.Use();
            anim.SetTrigger(equipWeapon.type == Weapon.Type.Melee ? "doSwing" : "doShot");
            fireDelay= 0;
        }
    }

    void Reload()
    {
        if(equipWeapon == null){
            return;
        }
        if(equipWeapon.type == Weapon.Type.Melee){
            return;
        }
        if(ammo == 0){
            return;
        }
        if(rDown && !isJump &&!isDodge && !isSwap && isFireReady && !isShop && !isDead){
            anim.SetTrigger("doReload");
            isReload = true;

            Invoke("ReloadOut", 3f);
        }
    }

    void ReloadOut()
    {
        int reAmmo = ammo < equipWeapon.maxAmmo ? ammo : equipWeapon.maxAmmo;
        equipWeapon.curAmmo = reAmmo;
        ammo -= reAmmo;
        isReload = false;
    }

    void Dodge()
    {
        //회피구현
        if(jDown && moveVec != Vector3.zero && !isJump && !isDodge && !isSwap && !isShop && !isDead){
            dodgeVec = moveVec;
            speed *=2;
            anim.SetTrigger("doDodge");
            isDodge = true;

            Invoke("DodgeOut", 0.5f);
        }
    }

    void DodgeOut()
    {
        speed *= 0.5f;
        isDodge = false;
    }

    void Swap()
    {
        if(sDown1 && (!hasWeapons[0] || equipWeaponIndex == 0)){
            return;
        }
        if(sDown2 && (!hasWeapons[1] || equipWeaponIndex == 1)){
            return;
        }
        if(sDown3 && (!hasWeapons[2] || equipWeaponIndex == 2)){
            return;
        }

        int weaponIndex = -1;
        if(sDown1) weaponIndex = 0;
        if(sDown2) weaponIndex = 1;
        if(sDown3) weaponIndex = 2;
        
        if((sDown1 || sDown2 || sDown3) && !isJump && !isDodge && !isShop && !isDead) {
            if(equipWeapon != null){
                equipWeapon.gameObject.SetActive(false);
            }

                equipWeaponIndex = weaponIndex;
                equipWeapon = weapons[weaponIndex].GetComponent<Weapon>();
                equipWeapon.gameObject.SetActive(true);

                anim.SetTrigger("doSwap");

                isSwap = true;

                Invoke("SwapOut", 0.4f);
        }
    }

    void SwapOut()
    {
        isSwap = false;
    }

    void Interation()
    {
        if(iDown && nearObject != null && !isJump && !isDodge && !isShop && !isDead){
            if(nearObject.tag == "Weapon"){
                Item item = nearObject.GetComponent<Item>();
                int weaponIndex = item.value;
                hasWeapons[weaponIndex] = true;

                Destroy(nearObject);
            }else if (nearObject.tag == "Shop"){
                Shop shop = nearObject.GetComponent<Shop>();
                shop.Enter(this);
                isShop = true;
            }
        }
    }

    void FreezeRotaion()
    {
        rigid.angularVelocity = Vector3.zero;
    }

    void StopToWall()
    {
        Debug.DrawRay(transform.position, transform.forward * 5, Color.green);
        isBorder = Physics.Raycast(transform.position, transform.forward, 5, LayerMask.GetMask("Wall"));
    }

    void FixedUpdate()
    {
        FreezeRotaion();
        StopToWall();
    }

    //착지 구현
    void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.tag == "Floor") {
            anim.SetBool("isJump", false);
            isJump = false;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Item"){
            Item item = other.GetComponent<Item>();
            switch (item.type) {
                case Item.Type.Ammo :
                    ammo += item.value;
                    if(ammo > maxAmmo){
                        ammo = maxAmmo;
                    }
                    break;
                case Item.Type.Coin :
                    coin += item.value;
                    if(coin > maxCoin){
                        coin = maxCoin;
                    }
                    break;
                case Item.Type.Heart :
                    health += item.value;
                    if(health > maxHealth){
                        health = maxHealth;
                    }
                    break;
                case Item.Type.Grenade:
                    if(hasGrenades == maxHasGrenades)
                        return;
                    grenades[hasGrenades ].SetActive(true);
                    hasGrenades  += item.value;
                    break;
            }

            Destroy(other.gameObject);

        }else if (other.tag == "EnemyBullet"){
            if(!isDamage){
                Bullet enemyBullet = other.GetComponent<Bullet>();
                health -= enemyBullet.damage;

                bool isBossAtk = other.name == "Boss Melee Area";

                StartCoroutine(OnDamage(isBossAtk));
            }

            if(other.GetComponent<Rigidbody>() !=null)
                    Destroy(other.gameObject);
        }
    }

    IEnumerator OnDamage(bool isBossAtk)
    {
        isDamage = true;
        foreach(MeshRenderer mesh in meshs){
            mesh.material.color = Color.yellow;
        }
        if(isBossAtk){
            rigid.AddForce(transform.forward * -25, ForceMode.Impulse);
        }

        if(health <= 0 && !isDead){
            OnDie();
        }

        yield return new WaitForSeconds(1f);

        isDamage = false;
        foreach(MeshRenderer mesh in meshs){
            mesh.material.color = Color.white;
        }

        if(isBossAtk){
            rigid.velocity = Vector3.zero;
        }

        
    }

    void OnDie()
    {
        anim.SetTrigger("doDie");
        isDead = true;
        manager.GameOver();
    }

    void OnTriggerStay(Collider other)
    {
        if(other.tag == "Weapon" || other.tag == "Shop"){
            nearObject = other.gameObject;
        }

        // Debug.Log(nearObject.name);
    }

    void OnTriggerExit(Collider other)
    {
        if(other.tag == "Weapon"){
            nearObject = null;
        }else if(other.tag == "Shop"){
            Shop shop = nearObject.GetComponent<Shop>();
            shop.Exit();
            isShop = false;
            nearObject = null;
        }
    }
}
