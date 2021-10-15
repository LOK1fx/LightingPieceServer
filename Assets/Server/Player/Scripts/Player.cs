using UnityEngine;

public class Player : MonoBehaviour
{
    public int Id { get; private set; }
    public string Username { get; private set; }

    private readonly float _moveSpeed = 5f;
    private bool[] _inputs;

    private CharacterController _characterController;

    public void Initialize(int id, string username)
    {
        Id = id;
        Username = username;

        _inputs = new bool[4];
    }

    private void Start()
    {
        _characterController = GetComponent<CharacterController>();
    }

    public void FixedUpdate()
    {
        Move(GetInputDirection());
    }

    public void SetInput(bool[] inputs, Quaternion rotation, long tick)
    {
        _inputs = inputs;

        transform.rotation = rotation;
    }

    private void Move(Vector2 inputDirection)
    {
        var forward = transform.forward;
        var right = transform.right;

        var moveDirection = right * inputDirection.x + forward * inputDirection.y;
        var move = moveDirection.normalized * (_moveSpeed / Constants.TICK_RATE);

        //transform.position += moveDirection.normalized * (_moveSpeed / Constants.TICK_RATE);

        _characterController.Move(move);

        ServerSend.PlayerPosition(this);
        ServerSend.PlayerRotation(this);
    }

    private Vector2 GetInputDirection()
    {
        var inputDir = Vector2.zero;

        if (_inputs[0])
        {
            inputDir.y += 1;
        }
        if (_inputs[1])
        {
            inputDir.x -= 1;
        }
        if (_inputs[2])
        {
            inputDir.y -= 1;
        }
        if (_inputs[3])
        {
            inputDir.x += 1;
        }

        return inputDir;
    }
}
