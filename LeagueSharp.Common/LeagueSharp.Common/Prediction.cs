﻿#region LICENSE

/*
 Copyright 2014 - 2014 LeagueSharp
 Prediction.cs is part of LeagueSharp.Common.
 
 LeagueSharp.Common is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.
 
 LeagueSharp.Common is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.
 
 You should have received a copy of the GNU General Public License
 along with LeagueSharp.Common. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

#region

using System;
using System.Collections.Generic;
using System.Linq;
using SharpDX;
using Color = System.Drawing.Color;

#endregion

namespace LeagueSharp.Common
{
    public enum HitChance
    {
        Dashing = 7,
        Immobile = 6,
        High = 5,
        Medium = 4,
        Low = 3,
        Impossible = 2,
        OutOfRange = 1,
        Collision = 0,
    }

    public enum SkillshotType
    {
        SkillshotLine,
        SkillshotCircle,
        SkillshotCone,
    }

    public enum CollisionableObjects
    {
        Minions,
        Heroes,
        YasuoWall,
        Walls,
    }

    public class PredictionInput
    {
        /// <summary>
        /// Set to true make the prediction hit as many enemy heroes as posible.
        /// </summary>
        public bool Aoe = false;

        /// <summary>
        /// Set to true if the unit collides with units.
        /// </summary>
        public bool Collision = false;

        /// <summary>
        /// Array that contains the unit types that the skillshot can collide with.
        /// </summary>
        public CollisionableObjects[] CollisionObjects =
        {
            CollisionableObjects.Minions, CollisionableObjects.YasuoWall
        };

        /// <summary>
        /// The skillshot delay in seconds.
        /// </summary>
        public float Delay = 0f;

        /// <summary>
        /// The skillshot width's radius or the angle in case of the cone skillshots.
        /// </summary>
        public float Radius = 1f;

        /// <summary>
        /// The skillshot range in units.
        /// </summary>
        public float Range = float.MaxValue;

        /// <summary>
        /// The skillshot speed in units per second.
        /// </summary>
        public float Speed = float.MaxValue;

        /// <summary>
        /// The skillshot type.
        /// </summary>
        public SkillshotType Type = SkillshotType.SkillshotLine;

        /// <summary>
        /// The unit that the prediction will made for.
        /// </summary>
        public Obj_AI_Base Unit = ObjectManager.Player;

        /// <summary>
        /// Set to true to increase the prediction radius by the unit bounding radius.
        /// </summary>
        public bool UseBoundingRadius = true;

        private Vector3 _from;

        private Vector3 _rangeCheckFrom;

        /// <summary>
        /// The position from where the skillshot missile gets fired.
        /// </summary>
        public Vector3 From
        {
            get { return _from.To2D().IsValid() ? _from : ObjectManager.Player.ServerPosition; }
            set { _from = value; }
        }

        /// <summary>
        /// The position from where the range is checked.
        /// </summary>
        public Vector3 RangeCheckFrom
        {
            get
            {
                return _rangeCheckFrom.To2D().IsValid()
                    ? _rangeCheckFrom
                    : (From.To2D().IsValid() ? From : ObjectManager.Player.ServerPosition);
            }
            set { _rangeCheckFrom = value; }
        }

        internal float RealRadius
        {
            get { return UseBoundingRadius ? Radius + Unit.BoundingRadius : Radius; }
        }
    }

    public class PredictionOutput
    {
        /// <summary>
        /// The list of the targets that the spell will hit (only if aoe was enabled).
        /// </summary>
        public List<Obj_AI_Hero> AoeTargetsHit = new List<Obj_AI_Hero>();

        /// <summary>
        /// The list of the units that the skillshot will collide with.
        /// </summary>
        public List<Obj_AI_Base> CollisionObjects = new List<Obj_AI_Base>();

        /// <summary>
        /// Returns the hitchance.
        /// </summary>
        public HitChance Hitchance = HitChance.Impossible;

        internal PredictionInput Input;

        internal int _aoeTargetsHitCount;
        private Vector3 _castPosition;

        private Vector3 _unitPosition;

        /// <summary>
        /// The position where the skillshot should be casted to increase the accuracy.
        /// </summary>
        public Vector3 CastPosition
        {
            get { return _castPosition.To2D().IsValid() ? _castPosition : Input.Unit.ServerPosition; }
            set { _castPosition = value; }
        }

        /// <summary>
        /// The number of targets the skillshot will hit (only if aoe was enabled).
        /// </summary>
        public int AoeTargetsHitCount
        {
            get { return Math.Max(_aoeTargetsHitCount, AoeTargetsHit.Count); }
        }

        /// <summary>
        /// The position where the unit is going to be when the skillshot reaches his position.
        /// </summary>
        public Vector3 UnitPosition
        {
            get { return _unitPosition.To2D().IsValid() ? _unitPosition : Input.Unit.ServerPosition; }
            set { _unitPosition = value; }
        }
    }

    /// <summary>
    /// Class used for calculating the position of the given unit after a delay.
    /// </summary>
    public static class Prediction
    {
        public static PredictionOutput GetPrediction(Obj_AI_Base unit, float delay)
        {
            return GetPrediction(new PredictionInput { Unit = unit, Delay = delay, });
        }

        public static PredictionOutput GetPrediction(Obj_AI_Base unit, float delay, float radius)
        {
            return GetPrediction(new PredictionInput { Unit = unit, Delay = delay, Radius = radius, });
        }

        public static PredictionOutput GetPrediction(Obj_AI_Base unit, float delay, float radius, float speed)
        {
            return GetPrediction(new PredictionInput { Unit = unit, Delay = delay, Radius = radius, Speed = speed, });
        }

        public static PredictionOutput GetPrediction(PredictionInput input)
        {
            return GetPrediction(input, true, true);
        }

        internal static PredictionOutput GetPrediction(PredictionInput input, bool ft, bool checkCollision)
        {
            PredictionOutput result = null;

            if (!input.Unit.IsValidTarget(float.MaxValue, false))
            {
                return new PredictionOutput();
            }

            if (ft)
            {
                //Increase the delay due to the latency and server tick:
                input.Delay += Game.Ping / 2000f + 0.05f;

                if (input.Aoe)
                {
                    return AoePrediction.GetPrediction(input);
                }
            }

            //Target too far away.
            if (input.Range != float.MaxValue && input.Unit.Distance(input.RangeCheckFrom) >= input.Range * 1.5d)
            {
                return new PredictionOutput { Input = input };
            }

            //Unit is dashing.
            if (input.Unit.IsDashing())
            {
                result = GetDashingPrediction(input);
            }
            else
            {
                //Unit is immobile.
                var remainingImmobileT = UnitIsImmobileUntil(input.Unit);
                if (remainingImmobileT >= 0d)
                {
                    result = GetImmobilePrediction(input, remainingImmobileT);
                }
            }
            
            //Normal prediction
            if (result == null)
            {
                result = GetStandardPrediction(input);
            }

            //Check if the unit position is in range
            if (input.Range != float.MaxValue)
            {
                if (result.Hitchance == HitChance.High &&
                    input.RangeCheckFrom.Distance(input.Unit.Position) > input.Range + input.RealRadius * 3 / 4)
                {
                    result.Hitchance = HitChance.Medium;
                }

                if (input.RangeCheckFrom.Distance(result.UnitPosition) >
                    input.Range + (input.Type == SkillshotType.SkillshotCircle ? input.RealRadius : 0))
                {
                    result.Hitchance = HitChance.OutOfRange;
                }

                if (input.RangeCheckFrom.Distance(result.CastPosition) > input.Range)
                {
                    if (result.Hitchance != HitChance.OutOfRange)
                    {
                        result.CastPosition = input.RangeCheckFrom +
                                              input.Range *
                                              (result.UnitPosition - input.RangeCheckFrom).To2D().Normalized().To3D();
                    }
                    else
                    {
                        result.Hitchance = HitChance.OutOfRange;
                    }
                }
            }

            //Check for collision
            if (checkCollision && input.Collision)
            {
                var positions = new List<Vector3> { result.UnitPosition, result.CastPosition, input.Unit.Position };
                result.CollisionObjects = Collision.GetCollision(positions, input);
                result.Hitchance = result.CollisionObjects.Count > 0 ? HitChance.Collision : result.Hitchance;
            }

            return result;
        }

        internal static PredictionOutput GetDashingPrediction(PredictionInput input)
        {
            var dashData = input.Unit.GetDashInfo();
            var result = new PredictionOutput { Input = input };
            input.Delay += 0.1f;
            //Normal dashes.
            if (!dashData.IsBlink)
            {
                //Mid air:
                var dashPred = GetPositionOnPath(
                    input, new List<Vector2> { input.Unit.ServerPosition.To2D(), dashData.Path.Last() }, dashData.Speed);
                if (dashPred.Hitchance == HitChance.High)
                {
                    dashPred.CastPosition = dashPred.UnitPosition;
                    dashPred.Hitchance = HitChance.Dashing;
                    return dashPred;
                }

                //At the end of the dash:
                if (dashData.Path.PathLength() > 200)
                {
                    var endP = dashData.Path.Last();
                    var timeToPoint = input.Delay + input.From.To2D().Distance(endP) / input.Speed;
                    if (timeToPoint <=
                        input.Unit.Distance(endP) / dashData.Speed + input.RealRadius / input.Unit.MoveSpeed)
                    {
                        return new PredictionOutput
                        {
                            CastPosition = endP.To3D(),
                            UnitPosition = endP.To3D(),
                            Hitchance = HitChance.Dashing,
                        };
                    }
                }

                result.CastPosition = dashData.Path.Last().To3D();
                result.UnitPosition = result.CastPosition;

                //Figure out where the unit is going.
            }

            return result;
        }

        internal static PredictionOutput GetImmobilePrediction(PredictionInput input, double remainingImmobileT)
        {
            var timeToReachTargetPosition = input.Delay + input.Unit.Distance(input.From) / input.Speed;

            if (timeToReachTargetPosition <= remainingImmobileT + input.RealRadius / input.Unit.MoveSpeed)
            {
                return new PredictionOutput
                {
                    CastPosition = input.Unit.ServerPosition,
                    UnitPosition = input.Unit.Position,
                    Hitchance = HitChance.Immobile,
                };
            }

            return new PredictionOutput
            {
                Input = input,
                CastPosition = input.Unit.ServerPosition,
                UnitPosition = input.Unit.ServerPosition,
                Hitchance = HitChance.High
                /*timeToReachTargetPosition - remainingImmobileT + input.RealRadius / input.Unit.MoveSpeed < 0.4d ? HitChance.High : HitChance.Medium*/,
            };
        }

        internal static PredictionOutput GetStandardPrediction(PredictionInput input)
        {
            var speed = input.Unit.MoveSpeed;

            if (input.Unit.Distance(input.From) < 300)
            {
                input.Delay /= 2;
                speed *= 2;
            }

            var result = GetPositionOnPath(input, input.Unit.GetWaypoints(), speed);

            if (result.Hitchance >= HitChance.High && input.Unit is Obj_AI_Hero) { }

            return result;
        }

        internal static double UnitIsImmobileUntil(Obj_AI_Base unit)
        {
            var result = 0d;
            foreach (var buff in unit.Buffs)
            {
                if (buff.IsActive && Game.Time <= buff.EndTime &&
                    (buff.Type == BuffType.Charm || buff.Type == BuffType.Knockup || buff.Type == BuffType.Stun ||
                     buff.Type == BuffType.Suppression || buff.Type == BuffType.Snare))
                {
                    result = Math.Max(result, buff.EndTime);
                }
            }
            return (result - Game.Time);
        }


        internal static PredictionOutput GetPositionOnPath(PredictionInput input, List<Vector2> path, float speed = -1)
        {
            speed = (speed == -1) ? input.Unit.MoveSpeed : speed;

            if (path.Count == 1)
            {
                var rTime = input.Delay + input.Unit.Distance(input.From) / input.Speed - input.RealRadius / speed -
                            0.15d;


                return new PredictionOutput
                {
                    Input = input,
                    UnitPosition = input.Unit.ServerPosition,
                    CastPosition = input.Unit.ServerPosition,
                    Hitchance = HitChance.High
                };
            }

            var pLength = path.PathLength();

            //Skillshots with only a delay
            if (pLength >= input.Delay * speed - input.RealRadius && input.Speed == float.MaxValue)
            {
                var tDistance = input.Delay * speed;

                for (var i = 0; i < path.Count - 1; i++)
                {
                    var a = path[i];
                    var b = path[i + 1];
                    var d = a.Distance(b);
                    if (d >= tDistance || i == path.Count - 2)
                    {
                        tDistance = (i == path.Count - 2 && d < tDistance) ? d : tDistance;

                        var direction = (b - a).Normalized();
                        var cp = a + direction * (tDistance - input.RealRadius);
                        var p = a + direction * tDistance;

                        if (input.Type == SkillshotType.SkillshotLine)
                        {
                            var alpha = (input.From.To2D() - p).AngleBetween(a - b);
                            if (alpha > 17 && alpha < 180 - 17)
                            {
                                var beta = (float)Math.Asin((input.RealRadius - 10) / p.Distance(input.From));
                                var cp1 = input.From.To2D() + (p - input.From.To2D()).Rotated(beta);
                                var cp2 = input.From.To2D() + (p - input.From.To2D()).Rotated(-beta);

                                cp = cp1.Distance(cp) < cp2.Distance(cp) ? cp1 : cp2;
                            }
                        }

                        return new PredictionOutput
                        {
                            Input = input,
                            CastPosition = cp.To3D(),
                            UnitPosition = p.To3D(),
                            Hitchance = HitChance.High,
                        };
                    }

                    tDistance -= d;
                }
            }

            //Skillshot with a delay and speed.
            if (pLength >= input.Delay * speed && input.Speed != float.MaxValue)
            {
                path = path.CutPath(Math.Max(0, input.Delay * speed));
                var tT = 0f;
                for (var i = 0; i < path.Count - 1; i++)
                {
                    var a = path[i];
                    var b = path[i + 1];
                    var tB = a.Distance(b) / speed;
                    var direction = (b - a).Normalized();
                    a = a - speed * tT * direction;
                    var sol = Geometry.VectorMovementCollision(a, b, speed, input.From.To2D(), input.Speed, tT);
                    var t = (float)sol[0];
                    var pos = (Vector2)sol[1];

                    if (pos.IsValid() && t >= tT && t <= tT + tB)
                    {
                        var cp = pos - input.RealRadius * direction;

                        if (input.Type == SkillshotType.SkillshotLine)
                        {
                            var alpha = (input.From.To2D() - pos).AngleBetween(a - b);
                            if (alpha > 17 && alpha < 180 - 17)
                            {
                                var beta = (float)Math.Asin(input.RealRadius / pos.Distance(input.From));
                                var cp1 = input.From.To2D() + (pos - input.From.To2D()).Rotated(beta);
                                var cp2 = input.From.To2D() + (pos - input.From.To2D()).Rotated(-beta);

                                cp = cp1.Distance(cp) < cp2.Distance(cp) ? cp1 : cp2;
                            }
                        }

                        return new PredictionOutput
                        {
                            Input = input,
                            CastPosition = cp.To3D(),
                            UnitPosition = pos.To3D(),
                            Hitchance = HitChance.High,
                        };
                    }
                    tT += tB;
                }
            }

            var position = path.Last();
            return new PredictionOutput
            {
                Input = input,
                CastPosition = position.To3D(),
                UnitPosition = position.To3D(),
                Hitchance = HitChance.Medium,
            };
        }
    }

    internal static class AoePrediction
    {
        public static PredictionOutput GetPrediction(PredictionInput input)
        {
            switch (input.Type)
            {
                case SkillshotType.SkillshotCircle:
                    return Circle.GetPrediction(input);
                case SkillshotType.SkillshotCone:
                    return Cone.GetPrediction(input);
                case SkillshotType.SkillshotLine:
                    return Line.GetPrediction(input);
            }
            return new PredictionOutput();
        }

        internal static List<PosibleTarget> GetPosibleTargets(PredictionInput input)
        {
            var result = new List<PosibleTarget>();
            foreach (
                var enemy in
                    ObjectManager.Get<Obj_AI_Hero>()
                        .Where(
                            h =>
                                h.NetworkId != input.Unit.NetworkId &&
                                h.IsValidTarget((input.Range + 200 + input.RealRadius), true, input.RangeCheckFrom)))
            {
                input.Unit = enemy;
                var prediction = Prediction.GetPrediction(input, false, false);
                if (prediction.Hitchance >= HitChance.High)
                {
                    result.Add(new PosibleTarget { Position = prediction.UnitPosition.To2D(), Unit = enemy });
                }
            }
            return result;
        }

        public static class Circle
        {
            public static PredictionOutput GetPrediction(PredictionInput input)
            {
                var mainTargetPrediction = Prediction.GetPrediction(input, false, true);
                var posibleTargets = new List<PosibleTarget>
                {
                    new PosibleTarget { Position = mainTargetPrediction.UnitPosition.To2D(), Unit = input.Unit }
                };

                if (mainTargetPrediction.Hitchance >= HitChance.Medium)
                {
                    //Add the posible targets  in range:
                    posibleTargets.AddRange(GetPosibleTargets(input));
                }

                while (posibleTargets.Count > 1)
                {
                    var mecCircle = MEC.GetMec(posibleTargets.Select(h => h.Position).ToList());

                    if (mecCircle.Radius <= input.RealRadius - 10 &&
                        Vector2.DistanceSquared(mecCircle.Center, input.RangeCheckFrom.To2D()) <
                        input.Range * input.Range)
                    {
                        return new PredictionOutput
                        {
                            AoeTargetsHit = posibleTargets.Select(h => (Obj_AI_Hero)h.Unit).ToList(),
                            CastPosition = mecCircle.Center.To3D(),
                            UnitPosition = mainTargetPrediction.UnitPosition,
                            Hitchance = mainTargetPrediction.Hitchance,
                            Input = input,
                            _aoeTargetsHitCount = posibleTargets.Count,
                        };
                    }

                    float maxdist = -1;
                    var maxdistindex = 1;
                    for (var i = 1; i < posibleTargets.Count; i++)
                    {
                        var distance = Vector2.DistanceSquared(posibleTargets[i].Position, posibleTargets[0].Position);
                        if (distance > maxdist || maxdist.CompareTo(-1) == 0)
                        {
                            maxdistindex = i;
                            maxdist = distance;
                        }
                    }
                    posibleTargets.RemoveAt(maxdistindex);
                }

                return mainTargetPrediction;
            }
        }

        public static class Cone
        {
            internal static int GetHits(Vector2 end, double range, float angle, List<Vector2> points)
            {
                var result = 0;
                foreach (var point in points)
                {
                    var edge1 = end.Rotated(-angle / 2);
                    var edge2 = edge1.Rotated(angle);
                    if (point.Distance(new Vector2()) < range && edge1.CrossProduct(point) > 0 &&
                        point.CrossProduct(edge2) > 0)
                    {
                        result++;
                    }
                }

                return result;
            }

            public static PredictionOutput GetPrediction(PredictionInput input)
            {
                var mainTargetPrediction = Prediction.GetPrediction(input, false, true);
                var posibleTargets = new List<PosibleTarget>
                {
                    new PosibleTarget { Position = mainTargetPrediction.UnitPosition.To2D(), Unit = input.Unit }
                };

                if (mainTargetPrediction.Hitchance >= HitChance.Medium)
                {
                    //Add the posible targets  in range:
                    posibleTargets.AddRange(GetPosibleTargets(input));
                }

                if (posibleTargets.Count > 1)
                {
                    var candidates = new List<Vector2>();

                    foreach (var target in posibleTargets)
                    {
                        target.Position = target.Position - input.From.To2D();
                    }

                    for (var i = 0; i < posibleTargets.Count; i++)
                    {
                        for (var j = 0; j < posibleTargets.Count; j++)
                        {
                            if (i != j)
                            {
                                var p = (posibleTargets[i].Position + posibleTargets[j].Position) * 0.5f;
                                if (!candidates.Contains(p))
                                {
                                    candidates.Add(p);
                                }
                            }
                        }
                    }

                    var bestCandidateHits = -1;
                    var bestCandidate = new Vector2();
                    var positionsList = posibleTargets.Select(t => t.Position).ToList();

                    foreach (var candidate in candidates)
                    {
                        var hits = GetHits(candidate, input.Range, input.Radius, positionsList);
                        if (hits > bestCandidateHits)
                        {
                            bestCandidate = candidate;
                            bestCandidateHits = hits;
                        }
                    }

                    if (bestCandidateHits > 1 && input.From.To2D().Distance(bestCandidate) > 50)
                    {
                        return new PredictionOutput
                        {
                            Hitchance = mainTargetPrediction.Hitchance,
                            _aoeTargetsHitCount = bestCandidateHits,
                            UnitPosition = mainTargetPrediction.UnitPosition,
                            CastPosition = bestCandidate.To3D(),
                            Input = input,
                        };
                    }
                }
                return mainTargetPrediction;
            }
        }

        public static class Line
        {
            internal static IEnumerable<Vector2> GetHits(Vector2 start, Vector2 end, double radius, List<Vector2> points)
            {
                return points.Where(p => p.Distance(start, end, true, true) <= radius * radius);
            }

            internal static Vector2[] GetCandidates(Vector2 from, Vector2 to, float radius, float range)
            {
                var middlePoint = (from + to) / 2;
                var intersections = Geometry.CircleCircleIntersection(
                    from, middlePoint, radius, from.Distance(middlePoint));

                var c1 = intersections[0];
                var c2 = intersections[1];

                c1 = from + range * (to - c1).Normalized();
                c2 = from + range * (to - c2).Normalized();

                return new[] { c1, c2 };
            }

            public static PredictionOutput GetPrediction(PredictionInput input)
            {
                var mainTargetPrediction = Prediction.GetPrediction(input, false, true);
                var posibleTargets = new List<PosibleTarget>
                {
                    new PosibleTarget { Position = mainTargetPrediction.UnitPosition.To2D(), Unit = input.Unit }
                };
                if (mainTargetPrediction.Hitchance >= HitChance.Medium)
                {
                    //Add the posible targets  in range:
                    posibleTargets.AddRange(GetPosibleTargets(input));
                }

                if (posibleTargets.Count > 1)
                {
                    var candidates = new List<Vector2>();
                    foreach (var target in posibleTargets)
                    {
                        var targetCandidates = GetCandidates(
                            input.From.To2D(), target.Position, (input.Radius), input.Range);
                        candidates.AddRange(targetCandidates);
                    }

                    var bestCandidateHits = -1;
                    var bestCandidate = new Vector2();
                    var bestCandidateHitPoints = new List<Vector2>();
                    var positionsList = posibleTargets.Select(t => t.Position).ToList();

                    foreach (var candidate in candidates)
                    {
                        if (GetHits(
                                input.From.To2D(), candidate, (input.Radius + input.Unit.BoundingRadius / 3 - 10),
                                new List<Vector2> { posibleTargets[0].Position }).Count() == 1)
                        {
                            var hits =
                                GetHits(
                                    input.From.To2D(), candidate, input.Radius,
                                    positionsList).ToList();
                            var hitsCount = hits.Count;
                            if (hitsCount >= bestCandidateHits)
                            {
                                bestCandidateHits = hitsCount;
                                bestCandidate = candidate;
                                bestCandidateHitPoints = hits.ToList();
                            }
                        }
                    }

                    if (bestCandidateHits > 1)
                    {
                        float maxDistance = -1;
                        Vector2 p1 = new Vector2(), p2 = new Vector2();

                        //Center the position
                        for (var i = 0; i < bestCandidateHitPoints.Count; i++)
                        {
                            for (var j = 0; j < bestCandidateHitPoints.Count; j++)
                            {
                                var startP = input.From.To2D();
                                var endP = bestCandidate;
                                var proj1 = positionsList[i].ProjectOn(startP, endP);
                                var proj2 = positionsList[j].ProjectOn(startP, endP);
                                var dist = Vector2.DistanceSquared(bestCandidateHitPoints[i], proj1.LinePoint) +
                                           Vector2.DistanceSquared(bestCandidateHitPoints[j], proj2.LinePoint);
                                if (dist >= maxDistance &&
                                    (proj1.LinePoint - positionsList[i]).AngleBetween(
                                        proj2.LinePoint - positionsList[j]) > 90)
                                {
                                    maxDistance = dist;
                                    p1 = positionsList[i];
                                    p2 = positionsList[j];
                                }
                            }
                        }

                        return new PredictionOutput
                        {
                            Hitchance = mainTargetPrediction.Hitchance,
                            _aoeTargetsHitCount = bestCandidateHits,
                            UnitPosition = mainTargetPrediction.UnitPosition,
                            CastPosition = ((p1 + p2) * 0.5f).To3D(),
                            Input = input,
                        };
                    }
                }

                return mainTargetPrediction;
            }
        }

        internal class PosibleTarget
        {
            public Vector2 Position;
            public Obj_AI_Base Unit;
        }
    }

    public static class Collision
    {
        private static int WallCastT;
        private static Vector2 YasuoWallCastedPos;

        static Collision()
        {
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Hero_OnProcessSpellCast;
        }

        private static void Obj_AI_Hero_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsValid && sender.Team != ObjectManager.Player.Team && args.SData.Name == "YasuoWMovingWall")
            {
                WallCastT = Environment.TickCount;
                YasuoWallCastedPos = sender.ServerPosition.To2D();
            }
        }

        /// <summary>
        /// Returns the list of the units that the skillshot will hit before reaching the set positions.
        /// </summary>
        public static List<Obj_AI_Base> GetCollision(List<Vector3> positions, PredictionInput input)
        {
            var result = new List<Obj_AI_Base>();

            foreach (var position in positions)
            {
                foreach (var objectType in input.CollisionObjects)
                {
                    switch (objectType)
                    {
                        case CollisionableObjects.Minions:
                            foreach (var minion in ObjectManager.Get<Obj_AI_Minion>())
                            {
                                if (minion.IsValidTarget(Math.Min(input.Range + input.Radius + 100, 2000), true, input.RangeCheckFrom))
                                {
                                    input.Unit = minion;
                                    var minionPrediction = Prediction.GetPrediction(input, false, false);
                                    if (
                                        minionPrediction.UnitPosition.To2D()
                                            .Distance(input.From.To2D(), position.To2D(), true, true) <=
                                        Math.Pow((input.Radius + 15 + minion.BoundingRadius), 2))
                                    {
                                        result.Add(minion);
                                    }
                                }
                            }
                            break;
                        case CollisionableObjects.Heroes:
                            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>())
                            {
                                if (hero.IsValidTarget(Math.Min(input.Range + input.Radius + 100, 2000), true, input.RangeCheckFrom))
                                {
                                    input.Unit = hero;
                                    var prediction = Prediction.GetPrediction(input, false, false);
                                    if (
                                        prediction.UnitPosition.To2D()
                                            .Distance(input.From.To2D(), position.To2D(), true, true) <=
                                        Math.Pow((input.Radius + 50 + hero.BoundingRadius), 2))
                                    {
                                        result.Add(hero);
                                    }
                                }
                            }
                            break;

                        case CollisionableObjects.Walls:
                            var step = position.Distance(input.From) / 20;
                            for (var i = 0; i < 20; i++)
                            {
                                var p = input.From.To2D().Extend(position.To2D(), step * i);
                                if (NavMesh.GetCollisionFlags(p.X, p.Y).HasFlag(CollisionFlags.Wall))
                                {
                                    result.Add(ObjectManager.Player);
                                }
                            }
                            break;

                        case CollisionableObjects.YasuoWall:

                        if (Environment.TickCount - WallCastT > 4000) break;

                        GameObject wall = null;
                        foreach (var gameObject in ObjectManager.Get<GameObject>())
                        {
                            if (gameObject.IsValid &&
                                System.Text.RegularExpressions.Regex.IsMatch(
                                    gameObject.Name, "_w_windwall_enemy_0.\\.troy",
                                    System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            {
                                wall = gameObject;
                            }
                        }
                        if (wall == null)
                        {
                            break;
                        }
                        var level = wall.Name.Substring(wall.Name.Length - 6, 1);
                        var wallWidth = (300 + 50 * Convert.ToInt32(level));

                        var wallDirection = (wall.Position.To2D() - YasuoWallCastedPos).Normalized().Perpendicular();
                        var wallStart = wall.Position.To2D() + wallWidth / 2 * wallDirection;
                        var wallEnd = wallStart - wallWidth * wallDirection;
                       
                        if (wallStart.Intersection(wallEnd, position.To2D(), input.From.To2D()).Intersects)
                        {
                            var t = Environment.TickCount + (wallStart.Intersection(wallEnd, position.To2D(), input.From.To2D())
                                    .Point.Distance(input.From) / input.Speed + input.Delay) * 1000;
                            if (t < WallCastT + 4000)
                            {
                                result.Add(ObjectManager.Player);
                            }
                        }
                                
                        break;
                    }
                }
            }

            return result.Distinct().ToList();
        }
    }

    internal class StoredPath
    {
        public List<Vector2> Path;
        public int Tick = 0;

        public double Time
        {
            get { return (Environment.TickCount - Tick) / 1000d; }
        }

        public int WaypointCount
        {
            get { return Path.Count; }
        }

        public Vector2 StartPoint
        {
            get { return Path.FirstOrDefault(); }
        }

        public Vector2 EndPoint
        {
            get { return Path.LastOrDefault(); }
        }
    }

    internal static class PathTracker
    {
        private const bool TrackAllies = true;
        private const double MaxTime = 1.5d;
        private static readonly Dictionary<int, List<StoredPath>> StoredPaths = new Dictionary<int, List<StoredPath>>();

        static PathTracker()
        {
            Obj_AI_Base.OnNewPath += Obj_AI_Hero_OnNewPath;
        }

        private static void Obj_AI_Hero_OnNewPath(Obj_AI_Base sender, GameObjectNewPathEventArgs args)
        {
            if (!TrackAllies && sender.Team != ObjectManager.Player.Team)
            {
                return;
            }

            if (!StoredPaths.ContainsKey(sender.NetworkId))
            {
                StoredPaths.Add(sender.NetworkId, new List<StoredPath>());
            }

            var newPath = new StoredPath { Tick = Environment.TickCount, Path = args.Path.ToList().To2D() };
            StoredPaths[sender.NetworkId].Add(newPath);
        }

        public static List<StoredPath> GetStoredPaths(Obj_AI_Base unit, double maxT)
        {
            return StoredPaths.ContainsKey(unit.NetworkId)
                ? StoredPaths[unit.NetworkId].Where(p => p.Time < maxT).ToList()
                : new List<StoredPath>();
        }

        public static StoredPath GetCurrentPath(Obj_AI_Base unit)
        {
            return StoredPaths.ContainsKey(unit.NetworkId)
                ? StoredPaths[unit.NetworkId].LastOrDefault()
                : new StoredPath();
        }

        public static Vector3 GetTendency(Obj_AI_Base unit)
        {
            var paths = GetStoredPaths(unit, MaxTime);
            var result = new Vector2();

            foreach (var path in paths)
            {
                var k = 1; //(MaxTime - path.Time);
                result = result + k * (path.EndPoint - unit.ServerPosition.To2D() /*path.StartPoint*/).Normalized();
            }

            result /= paths.Count;

            return result.To3D();
        }


        public static double GetMeanSpeed(Obj_AI_Base unit, double maxT)
        {
            var paths = GetStoredPaths(unit, MaxTime);
            var distance = 0d;
            if (paths.Count > 0)
            {
                //Assume that the unit was moving for the first path:
                distance += (maxT - paths[0].Time) * unit.MoveSpeed;

                for (var i = 0; i < paths.Count - 1; i++)
                {
                    var currentPath = paths[i];
                    var nextPath = paths[i + 1];

                    if (currentPath.WaypointCount > 0)
                    {
                        distance += Math.Min(
                            (currentPath.Time - nextPath.Time) * unit.MoveSpeed, currentPath.Path.PathLength());
                    }
                }

                //Take into account the last path:
                var lastPath = paths.Last();
                if (lastPath.WaypointCount > 0)
                {
                    distance += Math.Min(lastPath.Time * unit.MoveSpeed, lastPath.Path.PathLength());
                }
            }
            else
            {
                return unit.MoveSpeed;
            }


            return distance / maxT;
        }
    }
}