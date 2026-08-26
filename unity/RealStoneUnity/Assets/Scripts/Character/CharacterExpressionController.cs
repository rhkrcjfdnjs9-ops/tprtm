using UnityEngine;

namespace RealStone.Character
{
    public enum CharacterExpression
    {
        Neutral,
        Blink,
        Attack,
        Hit
    }

    [DisallowMultipleComponent]
    public sealed class CharacterExpressionController : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer eyesRenderer;
        [SerializeField] private SpriteRenderer mouthRenderer;
        [SerializeField] private Sprite eyesNeutral;
        [SerializeField] private Sprite eyesBlink;
        [SerializeField] private Sprite eyesAttack;
        [SerializeField] private Sprite eyesHit;
        [SerializeField] private Sprite mouthNeutral;
        [SerializeField] private Sprite mouthAttack;
        [SerializeField] private Sprite mouthHit;

        public CharacterExpression Current { get; private set; } = CharacterExpression.Neutral;

        public void Configure(SpriteRenderer eyes, SpriteRenderer mouth, Sprite neutralEyes, Sprite blinkEyes,
            Sprite attackEyes, Sprite hitEyes, Sprite neutralMouth, Sprite attackMouth, Sprite hitMouth)
        {
            eyesRenderer = eyes;
            mouthRenderer = mouth;
            eyesNeutral = neutralEyes;
            eyesBlink = blinkEyes;
            eyesAttack = attackEyes;
            eyesHit = hitEyes;
            mouthNeutral = neutralMouth;
            mouthAttack = attackMouth;
            mouthHit = hitMouth;
            SetNeutral();
        }

        public void SetNeutral() => SetExpression(CharacterExpression.Neutral);
        public void SetBlink() => SetExpression(CharacterExpression.Blink);
        public void SetAttack() => SetExpression(CharacterExpression.Attack);
        public void SetHit() => SetExpression(CharacterExpression.Hit);

        public void SetExpression(CharacterExpression expression)
        {
            Current = expression;
            if (eyesRenderer != null)
            {
                eyesRenderer.sprite = expression switch
                {
                    CharacterExpression.Blink => eyesBlink,
                    CharacterExpression.Attack => eyesAttack,
                    CharacterExpression.Hit => eyesHit,
                    _ => eyesNeutral
                };
            }

            if (mouthRenderer != null)
            {
                mouthRenderer.sprite = expression switch
                {
                    CharacterExpression.Attack => mouthAttack,
                    CharacterExpression.Hit => mouthHit,
                    _ => mouthNeutral
                };
            }
        }
    }
}
