using UnityEngine;
namespace Brotherhood
{
    // Prayer hits share the enemy hurt state and inventory hit events with the
    // sword. Geometry tests use the restored enemy body rather than its pivot.
    public static class PrayerCombat
    {
        public static Bounds EnemyBounds(EnemyController enemy)
        {
            float height=enemy.motor!=null?enemy.motor.size.y:1.15f;
            return new Bounds(enemy.transform.position+Vector3.up*height*.5f,new Vector3(enemy.Radius*2,height,2));
        }
        public static bool Hit(Bounds area,EnemyController enemy)=>enemy!=null&&!enemy.Dead&&area.Intersects(EnemyBounds(enemy));
        public static bool Circle(Vector2 center,float radius,EnemyController enemy)
        {
            if(enemy==null||enemy.Dead)return false;
            return Circle(center,radius,EnemyBounds(enemy));
        }
        public static bool Circle(Vector2 center,float radius,Bounds bounds)=>
            Vector2.SqrMagnitude((Vector2)bounds.ClosestPoint(center)-center)<=radius*radius;
        static Vector2[] Corners(Bounds bounds)=>new[]{new Vector2(bounds.min.x,bounds.min.y),new Vector2(bounds.max.x,bounds.min.y),new Vector2(bounds.max.x,bounds.max.y),new Vector2(bounds.min.x,bounds.max.y)};
        static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
        static bool Crossing(Vector2 a,Vector2 b,Vector2 c,Vector2 d)
        {
            Vector2 ab=b-a,cd=d-c;float denominator=Cross(ab,cd);
            if(Mathf.Abs(denominator)<.00001f)return false;
            float t=Cross(c-a,cd)/denominator,u=Cross(c-a,ab)/denominator;
            return t>=0&&t<=1&&u>=0&&u<=1;
        }
        static float PointSegment(Vector2 point,Vector2 a,Vector2 b)
        {
            Vector2 delta=b-a;
            return Vector2.Distance(point,a+delta*Mathf.Clamp01(Vector2.Dot(point-a,delta)/Mathf.Max(.00001f,delta.sqrMagnitude)));
        }
        public static bool Segment(Vector2 a,Vector2 b,float radius,EnemyController enemy)
        {
            if(enemy==null||enemy.Dead)return false;
            return Segment(a,b,radius,EnemyBounds(enemy));
        }
        public static bool Segment(Vector2 a,Vector2 b,float radius,Bounds bounds)
        {
            if(bounds.Contains(a)||bounds.Contains(b))return true;
            var corners=Corners(bounds);
            for(int i=0;i<4;i++)
            {
                Vector2 c=corners[i],d=corners[(i+1)%4];
                if(Crossing(a,b,c,d)||Mathf.Min(PointSegment(a,c,d),PointSegment(b,c,d),PointSegment(c,a,b),PointSegment(d,a,b))<=radius)return true;
            }
            return false;
        }
        public static bool Capsule(Vector2 center,Vector2 size,bool horizontal,EnemyController enemy)
        {
            return enemy!=null&&!enemy.Dead&&Capsule(center,size,horizontal,EnemyBounds(enemy));
        }
        public static bool Capsule(Vector2 center,Vector2 size,bool horizontal,Bounds bounds)
        {
            float radius=(horizontal?size.y:size.x)*.5f;
            Vector2 half=(horizontal?Vector2.right:Vector2.up)*Mathf.Max(0,(horizontal?size.x:size.y)*.5f-radius);
            return Segment(center-half,center+half,radius,bounds);
        }
        static bool Inside(Vector2 point,Vector2[] polygon)
        {
            bool inside=false;
            for(int i=0,j=polygon.Length-1;i<polygon.Length;j=i++)
            {
                Vector2 a=polygon[i],b=polygon[j];
                if((a.y>point.y)!=(b.y>point.y)&&point.x<(b.x-a.x)*(point.y-a.y)/(b.y-a.y)+a.x)inside=!inside;
            }
            return inside;
        }
        public static bool Polygon(Vector2[] worldPoints,EnemyController enemy)
        {
            if(enemy==null||enemy.Dead||worldPoints.Length<3)return false;
            return Polygon(worldPoints,EnemyBounds(enemy));
        }
        public static bool Polygon(Vector2[] worldPoints,Bounds bounds)
        {
            if(worldPoints==null||worldPoints.Length<3)return false;
            var corners=Corners(bounds);
            foreach(var point in worldPoints)if(bounds.Contains(point))return true;
            foreach(var point in corners)if(Inside(point,worldPoints))return true;
            for(int i=0;i<worldPoints.Length;i++)for(int j=0;j<4;j++)
                if(Crossing(worldPoints[i],worldPoints[(i+1)%worldPoints.Length],corners[j],corners[(j+1)%4]))return true;
            return false;
        }
        public static bool Polygon(Vector2[] points,Vector2 origin,float facing,EnemyController enemy)
        {
            var world=new Vector2[points.Length];for(int i=0;i<points.Length;i++)world[i]=origin+new Vector2(points[i].x*facing,points[i].y);
            return Polygon(world,enemy);
        }
        public static void Damage(BrotherhoodGame game,EnemyController enemy,float amount,bool heavy=false,bool gainFervour=true,bool trialMelee=false)
        {
            if(enemy==null||enemy.Dead||amount<=0)return;
            float before=enemy.health;
            if(heavy)game.player.HeavyWeaponHit(enemy,amount);
            else {enemy.Damage(amount,trialMelee);if(enemy.health<before)game.player.ItemEvent(2,amount);}
            if(gainFervour&&enemy.health<before&&(game.itemEffects==null||!game.itemEffects.HasTemporalFlag(0)))
            {
                var mods=new InventoryModifiers(game.progress,game.player);
                game.player.fervour=Mathf.Min(game.player.MaxFervour,game.player.fervour+mods.FervourGain(heavy?8:4));
            }
        }
        public static void Strike(BrotherhoodGame game,Bounds area)
        {
            if(game.world!=null)game.world.Strike(area);
        }
        public static void StrikeCircle(BrotherhoodGame game,Vector2 center,float radius)
        {
            if(game.world!=null)game.world.Strike(target=>Circle(center,radius,target));
        }
        public static void StrikeCapsule(BrotherhoodGame game,Vector2 center,Vector2 size,bool horizontal)
        {
            if(game.world!=null)game.world.Strike(target=>Capsule(center,size,horizontal,target));
        }
        public static void StrikePolygon(BrotherhoodGame game,Vector2[] points)
        {
            if(game.world!=null)game.world.Strike(target=>Polygon(points,target));
        }
        public static void StrikeBeam(BrotherhoodGame game,Vector2 from,Vector2 to,float width)
        {
            Vector2 offset=new Vector2(-(to-from).y,(to-from).x).normalized*width*.5f;
            StrikePolygon(game,new[]{from-offset,to-offset,to+offset,from+offset});
        }
    }
}
