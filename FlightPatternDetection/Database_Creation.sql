create table Flight
(
    Flight_Id    bigint         not null
        primary key
        unique,
    Registration VARCHAR(20) null,
    Model        VARCHAR(50) null,
    ICAO         CHAR(4)     not null,
    Mode_S       VARCHAR(10) null,
    Call_Sign    VARCHAR(10) null
);

create table Holding_Pattern
(
    Flight_Id    bigint                    not null
        primary key,
    Fixpoint     VARCHAR(15)            null,
    Laps         int                    null,
    Direction    ENUM ('RIGHT', 'LEFT') null,
    Leg_Distance double                 null,
    Altitude     double                 null,
    constraint Holding_Pattern_Flight_Flight_Id_fk
        foreign key (Flight_Id) references Flight (Flight_Id)
);

create table Airport
(
    ICAO      CHAR(4)      not null
        primary key
        unique,
    Name      VARCHAR(100) null,
    Country   VARCHAR(60)  null,
    Latitude  double       null,
    Longitude DOUBLE       null
);


create table Route_Information
(
    Flight_ID        bigint       null
        primary key,
    Destination_ICAO CHAR(4)   null,
    Origin_ICAO      CHAR(4)   null,
    Takeoff_Time     TIMESTAMP null,
    ATC_Route        LONGTEXT  null,
    constraint Route_Information_Airport_ICAO_fk
        foreign key (Destination_ICAO) references Airport (ICAO),
    constraint Route_Information_Airport_ICAO_fk_2
        foreign key (Origin_ICAO) references Airport (ICAO),
    constraint Route_Information_Flight_Flight_Id_fk
        foreign key (Flight_ID) references Flight (Flight_Id)
);


